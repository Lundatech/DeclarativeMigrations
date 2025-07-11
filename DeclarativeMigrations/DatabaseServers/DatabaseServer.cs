using System;
using System.Data;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;
using Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;
using Lundatech.DeclarativeMigrations.Models;

using Microsoft.Data.SqlClient;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers;

public class DatabaseServer {
    private readonly IDatabaseServer _databaseServer;
    private readonly DatabaseServerOptions _options = new();

    public DatabaseServer(DatabaseServerType databaseServerType, string connectionString, Action<DatabaseServerOptions>? configure = null) {
        _databaseServer = databaseServerType switch {
            DatabaseServerType.SqlServer => new SqlServerDatabaseServer(new SqlConnection(connectionString), false, null),
            DatabaseServerType.PostgreSql => new PostgreSqlDatabaseServer(new NpgsqlConnection(connectionString), false, null),
            _ => throw new NotSupportedException($"Database server type '{databaseServerType}' is not supported.")
        };

        if (configure != null) configure(_options);
    }

    public DatabaseServer(DatabaseServerType databaseServerType, IDbConnection connection, IDbTransaction? transaction = null, Action<DatabaseServerOptions>? configure = null) {
        _databaseServer = databaseServerType switch {
            DatabaseServerType.SqlServer => new SqlServerDatabaseServer((SqlConnection)connection, true, transaction != null ? (SqlTransaction)transaction : null),
            DatabaseServerType.PostgreSql => new PostgreSqlDatabaseServer((NpgsqlConnection)connection, true, transaction != null ? (NpgsqlTransaction)transaction : null),
            _ => throw new NotSupportedException($"Database server type '{databaseServerType}' is not supported.")
        };

        if (configure != null) configure(_options);
    }

    //public async Task<List<DatabaseSchema>> ReadAllSchemas() {
    //    return await _databaseServer.ReadAllSchemas();
    //}

    public async Task<DatabaseSchema> ReadSchema(string schemaName) {
        return await _databaseServer.ReadSchema(schemaName, _options);
    }

    public async Task ApplySchemaMigration(DatabaseSchemaMigration schemaMigration) {
        await _databaseServer.ApplySchemaMigration(schemaMigration, _options);
    }

    public async Task MigrateSchemaTo(DatabaseSchema targetSchema) {
        var databaseSchema = await ReadSchema(targetSchema.Name);
        var schemaMigration = databaseSchema.GetMigrationToTargetSchema(targetSchema, _options);
        await ApplySchemaMigration(schemaMigration);
    }
}
