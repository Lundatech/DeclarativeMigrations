using System;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    private (string TableName, string FullTableName) GetMigrationTableName(DatabaseServerOptions options, string schemaName, string tableName) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        return ($"{options.MigrationTablesPrefix}_{tableName}", $"\"{schemaName}\".\"{options.MigrationTablesPrefix}_{tableName}\"");
    }

    private async Task<bool> TableExists(string schemaName, string tableName) {
        var query = $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema_name AND table_name = @table_name);";
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("schema_name", schemaName);
        command.Parameters.AddWithValue("table_name", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is bool exists && exists;
    }

    private async Task<DatabaseSchema> ReadSchemaFromServer(string schemaName, DatabaseServerOptions options) {
        // read schema version from server
        var version = new Version(0, 0, 0);
        var (versionTableName, fullVersionTableName) = GetMigrationTableName(options, schemaName, "version");
        if (await TableExists(schemaName, versionTableName)) {
            var versionQuery = $"SELECT version FROM {fullVersionTableName} LIMIT 1;";
            await using var command = new NpgsqlCommand(versionQuery, _connection, _transaction);
            var result = await command.ExecuteScalarAsync();
            if (result != null && result is string versionString) {
                version = Version.Parse(versionString);
            }
        }

        var schema = new DatabaseSchema(schemaName, version);

        await ReadTables(schema, options);

        return schema;
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
                c.numeric_precision
            FROM information_schema.tables t
            JOIN information_schema.columns c
            	ON c.table_name = t.table_name AND c.table_schema = t.table_schema
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

            var isPrimaryKey = false;
            DatabaseTableColumnDefaultValue? defaultValue = null;
            DatabaseTableColumnForeignReference? foreignReference = null;
            
            // var (migrationTableName, fullMigrationTableName) = GetMigrationTableName(options, schema.Name, tableName);
            // if (await TableExists(schema.Name, migrationTableName)) {
            //     schema.Tables.Add(new DatabaseTable(tableName, fullMigrationTableName));
            // }

            if (currentTable == null) {
                currentTable = new DatabaseTable(schema, tableName);
            }
            else if (currentTable.Name != tableName) {
                schema.AddTable(currentTable);
                currentTable = new DatabaseTable(schema, tableName);
            }
            
            // add column
            var column = new DatabaseTableColumn(currentTable, columnName, GetDatabaseType(dataType, characterMaximumLength, numericPrecision), isNullable,
                isPrimaryKey, defaultValue, foreignReference);
            currentTable.AddColumn(column);
        }
    }

    private DatabaseType GetDatabaseType(string dataType, int? characterMaximumLength, int? numericPrecision) {
        switch (dataType) {
            case "character varying":
                return new DatabaseType(DatabaseType.Standard.String, length: characterMaximumLength);
            
            case "uuid":
                return new DatabaseType(DatabaseType.Standard.Guid);
            
            case "integer":
                return new DatabaseType(DatabaseType.Standard.Integer32);
            
            case "bigint":
                return new DatabaseType(DatabaseType.Standard.Integer64);
            
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