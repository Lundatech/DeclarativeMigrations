using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.Databases;

internal class PostgreSqlDatabaseServer : IDatabaseServer {
    private readonly NpgsqlConnection _connection;
    private bool _connectionIsOpened;
    private readonly NpgsqlTransaction? _transaction;

    public PostgreSqlDatabaseServer(NpgsqlConnection connection, bool connectionIsOpened, NpgsqlTransaction? transaction = null) {
        _connection = connection ?? throw new Exception("Connection cannot be null.");
        _connectionIsOpened = connectionIsOpened;
        _transaction = transaction;
    }

    public async Task<List<DatabaseSchema>> ReadAllSchemas() {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        throw new NotImplementedException();
    }

    public async Task<DatabaseSchema> ReadSchema(string schemaName) {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        throw new NotImplementedException();
    }

    public async Task ApplyMigration(DatabaseSchemaMigration migration, string migrationTemporaryStorageSchemaName, string migrationTemporaryStorageTablePrefix = "ltdm") {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        throw new NotImplementedException();
    }
}
