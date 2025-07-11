using System;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    private (string TableName, string FullTableName) GetTableName(DatabaseServerOptions options, string schemaName, string tableName) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        return ($"{options.MigrationTablesPrefix}_{tableName}", $"\"{schemaName}\".\"{options.MigrationTablesPrefix}_{tableName}\"");
    }

    private async Task<bool> TableExists(string schemaName, string tableName) {
        var query = $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @table_schema AND table_name = @table_name);";
        await using var command = new NpgsqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("table_schema", schemaName);
        command.Parameters.AddWithValue("table_name", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is bool exists && exists;
    }

    private async Task<DatabaseSchema> ReadSchemaFromServer(string schemaName, DatabaseServerOptions options) {
        // read schema version from server
        var version = new Version(0, 0, 0);
        var (versionTableName, fullVersionTableName) = GetTableName(options, schemaName, "version");
        if (await TableExists(schemaName, versionTableName)) {
            var versionQuery = $"SELECT version FROM {fullVersionTableName} LIMIT 1;";
            await using var command = new NpgsqlCommand(versionQuery, _connection, _transaction);
            var result = await command.ExecuteScalarAsync();
            if (result != null && result is string versionString) {
                version = Version.Parse(versionString);
            }
        }

        var schema = new DatabaseSchema(schemaName, version);

        //await ReadTables(schema, options);

        return schema;
    }
}