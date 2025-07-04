using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Microsoft.Data.SqlClient;

namespace Lundatech.DeclarativeMigrations.Databases;

internal class SqlServerDatabaseServer : IDatabaseServer {
    private readonly SqlConnection _connection;
    private bool _connectionIsOpened;
    private readonly IDbTransaction? _transaction;

    public SqlServerDatabaseServer(SqlConnection connection, bool connectionIsOpened, SqlTransaction? transaction = null) {
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
