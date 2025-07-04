using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Microsoft.Data.SqlClient;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.Databases;

public enum DatabaseServerType {
    SqlServer,
    PostgreSql
}

internal interface IDatabaseServer {
    Task<List<DatabaseSchema>> ReadAllSchemas();
    Task<DatabaseSchema> ReadSchema(string schemaName);
    Task ApplySchemaMigration(DatabaseSchemaMigration schemaMigration);
}

public class DatabaseServer {
    private readonly IDatabaseServer _databaseServer;

    public DatabaseServer(DatabaseServerType databaseServerType, string connectionString) {
        _databaseServer = databaseServerType switch {
            DatabaseServerType.SqlServer => new SqlServerDatabaseServer(new SqlConnection(connectionString), false, null),
            DatabaseServerType.PostgreSql => new PostgreSqlDatabaseServer(new NpgsqlConnection(connectionString), false, null),
            _ => throw new NotSupportedException($"Database server type '{databaseServerType}' is not supported.")
        };
    }

    public DatabaseServer(DatabaseServerType databaseServerType, IDbConnection connection, IDbTransaction? transaction = null) {
        _databaseServer = databaseServerType switch {
            DatabaseServerType.SqlServer => new SqlServerDatabaseServer((SqlConnection)connection, true, transaction != null ? (SqlTransaction)transaction : null),
            DatabaseServerType.PostgreSql => new PostgreSqlDatabaseServer((NpgsqlConnection)connection, true, transaction != null ? (NpgsqlTransaction)transaction : null),
            _ => throw new NotSupportedException($"Database server type '{databaseServerType}' is not supported.")
        };
    }

    public async Task<List<DatabaseSchema>> ReadAllSchemas() {
        return await _databaseServer.ReadAllSchemas();
    }

    public async Task<DatabaseSchema> ReadSchema(string schemaName) {
        return await _databaseServer.ReadSchema(schemaName);
    }

    public async Task ApplySchemaMigration(DatabaseSchemaMigration schemaMigration) {
        await _databaseServer.ApplySchemaMigration(schemaMigration);
    }

    public async Task MigrateSchemaTo(DatabaseSchema targetSchema, string? migrationTemporaryStorageSchemaName = null, string migrationTemporaryStorageTablePrefix = "ltdm") {
        var databaseSchema = await ReadSchema(targetSchema.Name);
        var schemaMigration = databaseSchema.GetMigrationToTargetSchema(targetSchema, migrationTemporaryStorageSchemaName, migrationTemporaryStorageTablePrefix);
        await ApplySchemaMigration(schemaMigration);
    }
}
