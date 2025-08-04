using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    private (string TableName, string FullTableName) GetMigrationTableName(DatabaseServerOptions options, string schemaName, string tableName) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        return ($"{options.MigrationDatabasePrefix}_{tableName}", $"\"{schemaName}\".\"{options.MigrationDatabasePrefix}_{tableName}\"");
    }

    private async Task<bool> SchemaExists(string schemaName) {
        var query = "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema_name)";
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schemaName);
        var result = await command.ExecuteScalarAsync();
        return result is bool and true;
    }
    
    private async Task<bool> TableExists(string schemaName, string tableName) {
        var query = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema_name AND table_name = @table_name)";
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schemaName);
        command.Parameters.AddWithValue("table_name", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is bool and true;
    }

    public override async Task<DatabaseSchema> ReadSchema(string schemaName, DatabaseServerOptions options) {
        // read schema version from server
        var version = new Version(0, 0, 0);
        var (versionTableName, fullVersionTableName) = GetMigrationTableName(options, schemaName, "version");

        if (await TableExists(schemaName, versionTableName)) {
            await using var command = new NpgsqlCommand($"SELECT version FROM {fullVersionTableName} LIMIT 1", _connection, _transaction);
            var result = await command.ExecuteScalarAsync();

            if (result != null && result is string versionString) {
                version = Version.Parse(versionString);
            }
        }

        var schema = new DatabaseSchema(DatabaseServerType.PostgreSql, schemaName, version);

        await ReadSequences(schema, options);
        await ReadTables(schema, options);
        await ReadIndexes(schema, options);

        return schema;
    }

    private async Task ReadSequences(DatabaseSchema schema, DatabaseServerOptions options) {
        var query = """
            SELECT
            	s.sequence_name
            FROM information_schema.sequences s
            WHERE s.sequence_schema = @schema_name
            """;
           await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schema.Name);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var sequenceName = (string)reader["sequence_name"];

            var sequence = new DatabaseSequence(schema, sequenceName);
            schema.AddSequence(sequence);
        }
    }
    
    private class PrimaryKeyConstraint {
        public string ConstraintName { get; }
        public string TableName { get; }
        public HashSet<string> ColumnNames { get; } = [];

        public PrimaryKeyConstraint(string constraintName, string tableName) {
            ConstraintName = constraintName;
            TableName = tableName;
        }
    }

    private class UniqueConstraint {
        public string ConstraintName { get; }
        public string TableName { get; }
        public HashSet<string> ColumnNames { get; } = [];

        public UniqueConstraint(string constraintName, string tableName) {
            ConstraintName = constraintName;
            TableName = tableName;
        }
    }

    private class ForeignKeyConstraint {
        public string ReferencedTableName { get; }
        public string ReferencedColumnName { get; }
        public CascadeType OnUpdateCascadeType { get; }
        public CascadeType OnDeleteCascadeType { get; }

        public ForeignKeyConstraint(string referencedTableName, string referencedColumnName, CascadeType onUpdateCascadeType, CascadeType onDeleteCascadeType) {
            ReferencedTableName = referencedTableName ?? throw new ArgumentNullException(nameof(referencedTableName), "Referenced table name cannot be null.");
            ReferencedColumnName = referencedColumnName ?? throw new ArgumentNullException(nameof(referencedColumnName), "Referenced column name cannot be null.");
            OnUpdateCascadeType = onUpdateCascadeType;
            OnDeleteCascadeType = onDeleteCascadeType;
        }
    }

    private async Task<Dictionary<string, PrimaryKeyConstraint>> ReadPrimaryKeyConstraints(DatabaseSchema schema, DatabaseServerOptions options) {
        var constraints = new Dictionary<string, PrimaryKeyConstraint>();

        var query = """
            SELECT
            	tc.constraint_name,
            	tc.table_name,
            	kcu.column_name
            FROM information_schema.table_constraints tc
            LEFT JOIN information_schema.key_column_usage kcu
            	ON kcu.constraint_schema = tc.constraint_schema AND kcu.constraint_name = tc.constraint_name
            WHERE tc.table_schema = @schema_name AND tc.constraint_type = 'PRIMARY KEY'
            ORDER BY tc.constraint_name
            """;
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schema.Name);

        PrimaryKeyConstraint? currentConstraint = null;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var constraintName = (string)reader["constraint_name"];
            var tableName = (string)reader["table_name"];
            var columnName = (string)reader["column_name"];

            if (tableName.StartsWith($"{options.MigrationDatabasePrefix}_")) {
                // skip migration tables
                continue;
            }

            if (currentConstraint == null) {
                currentConstraint = new PrimaryKeyConstraint(constraintName, tableName);
            }
            else if (currentConstraint.ConstraintName != constraintName) {
                constraints.Add(currentConstraint.TableName, currentConstraint);
                currentConstraint = new PrimaryKeyConstraint(constraintName, tableName);
            }

            // add column
            currentConstraint.ColumnNames.Add(columnName);
        }

        if (currentConstraint != null) constraints.Add(currentConstraint.TableName, currentConstraint);

        return constraints;
    }

    private async Task<Dictionary<string, UniqueConstraint>> ReadUniqueConstraints(DatabaseSchema schema, DatabaseServerOptions options) {
        var constraints = new Dictionary<string, UniqueConstraint>();

        var query = """
            SELECT
            	tc.constraint_name,
            	tc.table_name,
            	kcu.column_name
            FROM information_schema.table_constraints tc
            LEFT JOIN information_schema.key_column_usage kcu
            	ON kcu.constraint_schema = tc.constraint_schema AND kcu.constraint_name = tc.constraint_name
            WHERE tc.table_schema = @schema_name AND tc.constraint_type = 'UNIQUE'
            ORDER BY tc.constraint_name
            """;
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schema.Name);

        UniqueConstraint? currentConstraint = null;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var constraintName = (string)reader["constraint_name"];
            var tableName = (string)reader["table_name"];
            var columnName = (string)reader["column_name"];

            if (tableName.StartsWith($"{options.MigrationDatabasePrefix}_")) {
                // skip migration tables
                continue;
            }

            if (currentConstraint == null) {
                currentConstraint = new UniqueConstraint(constraintName, tableName);
            }
            else if (currentConstraint.ConstraintName != constraintName) {
                constraints.Add(currentConstraint.TableName, currentConstraint);
                currentConstraint = new UniqueConstraint(constraintName, tableName);
            }

            // add column
            currentConstraint.ColumnNames.Add(columnName);
        }

        if (currentConstraint != null) constraints.Add(currentConstraint.TableName, currentConstraint);

        return constraints;
    }

    private async Task<ConcurrentDictionary<string, Dictionary<string, ForeignKeyConstraint>>> ReadForeignKeyConstraints(DatabaseSchema schema, DatabaseServerOptions options) {
        var constraints = new ConcurrentDictionary<string, Dictionary<string, ForeignKeyConstraint>>();

        var query = """
            SELECT
            	tc.constraint_name,
            	tc.table_name,
            	kcu.column_name,
            	ccu.table_name AS referencing_table_name,
            	ccu.column_name AS referencing_column_name,
            	rc.update_rule,
            	rc.delete_rule
            FROM information_schema.table_constraints tc
            LEFT JOIN information_schema.constraint_column_usage ccu
            	ON ccu.constraint_schema = tc.constraint_schema AND ccu.constraint_name = tc.constraint_name
            LEFT JOIN information_schema.key_column_usage kcu
            	ON kcu.constraint_schema = tc.constraint_schema AND kcu.constraint_name = tc.constraint_name
            LEFT JOIN information_schema.referential_constraints rc
            	ON rc.constraint_schema = tc.constraint_schema AND rc.constraint_name = tc.constraint_name
            WHERE tc.table_schema = @schema_name AND tc.constraint_type = 'FOREIGN KEY'
            """;
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schema.Name);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var constraintName = (string)reader["constraint_name"];
            var tableName = (string)reader["table_name"];
            var columnName = (string)reader["column_name"];
            var referencingTableName = (string)reader["referencing_table_name"];
            var referencingColumnName = (string)reader["referencing_column_name"];
            var updateRule = (string)reader["update_rule"];
            var deleteRule = (string)reader["delete_rule"];

            if (tableName.StartsWith($"{options.MigrationDatabasePrefix}_")) {
                // skip migration tables
                continue;
            }

            var onUpdateCascadeType = updateRule switch {
                "RESTRICT" => CascadeType.Restrict,
                "CASCADE" => CascadeType.Cascade,
                "SET DEFAULT" => CascadeType.SetDefault,
                "SET NULL" => CascadeType.SetNull,
                "NO ACTION" => CascadeType.NoAction,
                _ => throw new NotImplementedException(updateRule),
            };

            var onDeleteCascadeType = deleteRule switch {
                "RESTRICT" => CascadeType.Restrict,
                "CASCADE" => CascadeType.Cascade,
                "SET DEFAULT" => CascadeType.SetDefault,
                "SET NULL" => CascadeType.SetNull,
                "NO ACTION" => CascadeType.NoAction,
                _ => throw new NotImplementedException(deleteRule),
            };

            var constraint = new ForeignKeyConstraint(referencingTableName, referencingColumnName, onUpdateCascadeType, onDeleteCascadeType);

            var tableConstraints = constraints.GetOrAdd(tableName, _ => []);
            tableConstraints[columnName] = constraint;
        }

        return constraints;
    }

    private async Task ReadTables(DatabaseSchema schema, DatabaseServerOptions options) {
        var primaryKeyConstraints = await ReadPrimaryKeyConstraints(schema, options);
        var uniqueConstraints = await ReadUniqueConstraints(schema, options);
        var foreignKeyConstraints = await ReadForeignKeyConstraints(schema, options);

        // read all tables from server
        var query = """
            SELECT
            	t.table_name,
            	c.column_name,
            	c.column_default,
            	c.is_nullable,
            	c.data_type,
            	c.character_maximum_length,
            	c.numeric_precision
            FROM information_schema.tables t
            JOIN information_schema.columns c
            	ON c.table_schema = t.table_schema AND c.table_name = t.table_name
            WHERE t.table_schema = @schema_name
            ORDER by t.table_name ASC, c.ordinal_position ASC
            """;
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schema.Name);

        DatabaseTable? currentTable = null;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var tableName = (string)reader["table_name"];
            var columnName = (string)reader["column_name"];
            var columnDefault = reader["column_default"] as string;
            var isNullable = (string)reader["is_nullable"] == "YES";
            var dataType = (string)reader["data_type"];
            var characterMaximumLength = reader["character_maximum_length"] as int?;
            var numericPrecision = reader["numeric_precision"] as int?;

            if (tableName.StartsWith($"{options.MigrationDatabasePrefix}_")) {
                // skip migration tables
                continue;
            }
            
            DatabaseType databaseType = GetDatabaseType(dataType, columnDefault, characterMaximumLength, numericPrecision);

            DatabaseTableColumnDefaultValue? defaultValue = null;
            if (columnDefault != null) defaultValue = GetColumnDefault(columnDefault, databaseType);

            if (currentTable == null) {
                currentTable = new DatabaseTable(schema, tableName);
            }
            else if (currentTable.Name != tableName) {
                schema.AddTable(currentTable);
                currentTable = new DatabaseTable(schema, tableName);
            }

            var isPrimaryKey = primaryKeyConstraints.TryGetValue(tableName, out var primaryKeyConstraint) && primaryKeyConstraint.ColumnNames.Contains(columnName);
            var isUnique = uniqueConstraints.TryGetValue(tableName, out var uniqueConstraint) && uniqueConstraint.ColumnNames.Contains(columnName);
            DatabaseTableColumnForeignReference? foreignReference = null;
            if (foreignKeyConstraints.TryGetValue(tableName, out var foreignKeyConstraint) && foreignKeyConstraint.TryGetValue(columnName, out var foreignKey)) {
                foreignReference = new DatabaseTableColumnForeignReference(foreignKey.ReferencedTableName, foreignKey.ReferencedColumnName, foreignKey.OnDeleteCascadeType);
            }

            // add column
            var column = new DatabaseTableColumn(currentTable, columnName, databaseType, isNullable, isPrimaryKey, isUnique, defaultValue, foreignReference);
            currentTable.AddColumn(column);
        }

        // add the table we were working on 
        if (currentTable != null) schema.AddTable(currentTable);
    }

    private async Task ReadIndexes(DatabaseSchema schema, DatabaseServerOptions options) {
        // read all indices from server
        var query = """
            SELECT
            	(i.indexrelid::regclass)::text AS index_name,
            	c.relname AS table_name,
            	a.attname AS column_name
            FROM (SELECT UNNEST(indkey) AS indkey, indexrelid, indrelid, indisprimary, indisunique FROM pg_index) i
            JOIN pg_class c
            	ON c.oid = i.indrelid
            JOIN pg_namespace ns
            	ON ns.oid = c.relnamespace
            JOIN pg_attribute a
            	ON a.attrelid = c.oid AND a.attnum = i.indkey
            WHERE ns.nspname = @schema_name AND i.indisprimary = false AND i.indisunique = false
            ORDER BY index_name
            """;
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schema.Name);

        string? currentIndexName = null;
        string? currentTableName = null;
        List<string>? currentIndexColumns = null;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var indexName = (string)reader["index_name"];

            // remove schema prefix if present
            if (indexName.Contains('.')) indexName = indexName[(indexName.LastIndexOf('.') + 1)..];

            var tableName = (string)reader["table_name"];
            var columnName = (string)reader["column_name"];

            if (currentIndexName == null) {
                currentIndexName = indexName;
                currentTableName = tableName;
                currentIndexColumns = [];
            }
            else if (currentIndexName != indexName) {
                var parentTable = schema.Tables[currentTableName!];
                var index = new DatabaseTableIndex(parentTable, currentIndexName, currentIndexColumns!);
                parentTable.AddIndex(index);

                currentIndexName = indexName;
                currentTableName = tableName;
                currentIndexColumns = [];
            }

            currentIndexColumns!.Add(columnName);
        }

        // add the index we were working on 
        if (currentIndexName != null) {
            var parentTable = schema.Tables[currentTableName!];
            var index = new DatabaseTableIndex(parentTable, currentIndexName, currentIndexColumns!);
            parentTable.AddIndex(index);
        }
    }

    private string NormalizeDefaultString(string databaseString) {
        return string.Join(' ', databaseString.ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Replace(" (", "(");
    }

    private DatabaseTableColumnDefaultValue? GetColumnDefault(string columnDefault, DatabaseType databaseType) {
        var normalizedDefault = NormalizeDefaultString(columnDefault);

        switch (databaseType.Type) {
            case DatabaseType.Standard.Guid:
                if (normalizedDefault == "GEN_RANDOM_UUID()")
                    return new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.RandomGuid);
                else if (normalizedDefault.StartsWith("'") && normalizedDefault.EndsWith("'::UUID") && Guid.TryParse(normalizedDefault[1..^7], out var guidValue))
                    return new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.FixedGuid, guidValue: guidValue);
                else
                    return null;

            case DatabaseType.Standard.Boolean:
                if (normalizedDefault == "TRUE")
                    return new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.FixedBoolean, booleanValue: true);
                else if (normalizedDefault == "FALSE")
                    return new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.FixedBoolean, booleanValue: false);
                else
                    return null;

            case DatabaseType.Standard.DateTime:
            case DatabaseType.Standard.DateTimeOffset:
                if (normalizedDefault == "(NOW() AT TIME ZONE 'UTC'::TEXT)")
                    return new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.CurrentDateTimeUtc);
                else if (normalizedDefault == "(NOW())")
                    return new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.CurrentDateTime);
                else
                    return null;

            default:
                return null;
        }
    }

    private DatabaseType GetDatabaseType(string dataType, string? columnDefault, int? characterMaximumLength, int? numericPrecision) {
        switch (dataType) {
            case "character varying":
                return new DatabaseType(DatabaseType.Standard.String, length: characterMaximumLength);

            case "uuid":
                return new DatabaseType(DatabaseType.Standard.Guid);

            case "integer": {
                if (columnDefault == null) return new DatabaseType(DatabaseType.Standard.Integer32);
                
                var normalizedDefault = NormalizeDefaultString(columnDefault);
                var isSequence = normalizedDefault.StartsWith("NEXTVAL('") && normalizedDefault.EndsWith("'::REGCLASS)");
                return isSequence ?
                    new DatabaseType(DatabaseType.Standard.SerialInteger32) :
                    new DatabaseType(DatabaseType.Standard.Integer32);
            }

            case "bigint": {
                if (columnDefault == null) return new DatabaseType(DatabaseType.Standard.Integer64);
                
                var normalizedDefault = NormalizeDefaultString(columnDefault);
                var isSequence = normalizedDefault.StartsWith("NEXTVAL('") && normalizedDefault.EndsWith("'::REGCLASS)");
                return isSequence ?
                    new DatabaseType(DatabaseType.Standard.SerialInteger64) :
                    new DatabaseType(DatabaseType.Standard.Integer64);
            }

            case "boolean":
                return new DatabaseType(DatabaseType.Standard.Boolean);

            case "text":
                return new DatabaseType(DatabaseType.Standard.String);

            case "bytea":
                return new DatabaseType(DatabaseType.Standard.Binary);

            case "timestamp with time zone":
                return new DatabaseType(DatabaseType.Standard.DateTimeOffset);

            case "interval":
                return new DatabaseType(DatabaseType.Standard.TimeSpan);

            case "oid":
                return new DatabaseType(DatabaseType.Standard.DatabaseObjectId);

            default:
                throw new NotImplementedException(dataType);
        }
    }
}