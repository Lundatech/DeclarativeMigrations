using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    private (string TableName, string FullTableName) GetMigrationTableName(DatabaseServerOptions options, string schemaName, string tableName) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        return ($"_{options.MigrationDatabasePrefix}_{tableName}", $"\"{schemaName}\".\"_{options.MigrationDatabasePrefix}_{tableName}\"");
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
    
    private async Task ReadTables(DatabaseSchema schema, DatabaseServerOptions options) {
        // read all tables from server
        var query = """
            SELECT
            	t.table_name,
            	c.column_name,
            	c.column_default,
            	c.is_nullable,
            	c.data_type,
            	c.character_maximum_length,
            	c.numeric_precision,
            	(SELECT EXISTS (
            		SELECT 
            			1
            		FROM information_schema.table_constraints tc
            		JOIN information_schema.constraint_column_usage ccu
            			ON ccu.constraint_schema = tc.constraint_schema AND ccu.constraint_name = tc.constraint_name
            		WHERE tc.table_schema = t.table_schema AND tc.table_name = t.table_name AND ccu.column_name = c.column_name AND tc.constraint_type = 'UNIQUE'
            	)) AS is_unique,
            	(SELECT EXISTS (
            		SELECT 
            			1
            		FROM information_schema.table_constraints tc
            		JOIN information_schema.constraint_column_usage ccu
            			ON ccu.constraint_schema = tc.constraint_schema AND ccu.constraint_name = tc.constraint_name
            		WHERE tc.table_schema = t.table_schema AND tc.table_name = t.table_name AND ccu.column_name = c.column_name AND tc.constraint_type = 'PRIMARY KEY'
            	)) AS is_primary_key,
            	(SELECT
            		ccu.table_name
            	FROM information_schema.table_constraints tc
            	JOIN information_schema.constraint_column_usage ccu
            		ON ccu.constraint_schema = tc.constraint_schema AND ccu.constraint_name = tc.constraint_name
            	WHERE tc.table_schema = t.table_schema AND tc.table_name = t.table_name AND ccu.column_name = c.column_name AND tc.constraint_type = 'FOREIGN KEY'
            	) AS foreign_table_name,
            	(SELECT
            		ccu.column_name
            	FROM information_schema.table_constraints tc
            	JOIN information_schema.constraint_column_usage ccu
            		ON ccu.constraint_schema = tc.constraint_schema AND ccu.constraint_name = tc.constraint_name
            	WHERE tc.table_schema = t.table_schema AND tc.table_name = t.table_name AND ccu.column_name = c.column_name AND tc.constraint_type = 'FOREIGN KEY'
            	) AS foreign_column_name,
            	(SELECT
            		rc.delete_rule
            	FROM information_schema.table_constraints tc
            	JOIN information_schema.constraint_column_usage ccu
            		ON ccu.constraint_schema = tc.constraint_schema AND ccu.constraint_name = tc.constraint_name
            	JOIN information_schema.referential_constraints rc
            		ON rc.constraint_schema = tc.constraint_schema AND rc.constraint_name = tc.constraint_name
            	WHERE tc.table_schema = t.table_schema AND tc.table_name = t.table_name AND ccu.column_name = c.column_name AND tc.constraint_type = 'FOREIGN KEY'
            	) AS foreign_delete_rule	
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
            var isPrimaryKey = (bool)reader["is_primary_key"];
            var isUnique = (bool)reader["is_unique"];
            var foreignTableName = reader["foreign_table_name"] as string;
            var foreignColumnName = reader["foreign_column_name"] as string;
            var foreignDeleteRule = reader["foreign_delete_rule"] as string;

            if (tableName.StartsWith($"_{options.MigrationDatabasePrefix}_")) {
                // skip migration tables
                continue;
            }
            
            DatabaseType databaseType = GetDatabaseType(dataType, columnDefault, characterMaximumLength, numericPrecision);

            DatabaseTableColumnDefaultValue? defaultValue = null;
            if (columnDefault != null) defaultValue = GetColumnDefault(columnDefault, databaseType);

            DatabaseTableColumnForeignReference? foreignReference = null;

            if (foreignDeleteRule != null) {
                var onDeleteCascadeType = foreignDeleteRule switch {
                    "RESTRICT" => CascadeType.Restrict,
                    "CASCADE" => CascadeType.Cascade,
                    "SET DEFAULT" => CascadeType.SetDefault,
                    "SET NULL" => CascadeType.SetNull,
                    "NO ACTION" => CascadeType.NoAction,
                    _ => throw new NotImplementedException(foreignDeleteRule),
                };
                foreignReference = new DatabaseTableColumnForeignReference(foreignTableName!, foreignColumnName!, onDeleteCascadeType);
            }

            if (currentTable == null) {
                currentTable = new DatabaseTable(schema, tableName);
            }
            else if (currentTable.Name != tableName) {
                schema.AddTable(currentTable);
                currentTable = new DatabaseTable(schema, tableName);
            }

            // add column
            var column = new DatabaseTableColumn(currentTable, columnName, databaseType, isNullable,
                isPrimaryKey, isUnique, defaultValue, foreignReference);
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