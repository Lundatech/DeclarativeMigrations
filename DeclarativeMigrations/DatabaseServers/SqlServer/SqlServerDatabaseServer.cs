using System;
using System.Data;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Microsoft.Data.SqlClient;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer : IDatabaseServer {
    private readonly SqlConnection _connection;
    private bool _connectionIsOpened;
    private readonly IDbTransaction? _transaction;

    public SqlServerDatabaseServer(SqlConnection connection, bool connectionIsOpened, SqlTransaction? transaction = null) {
        _connection = connection ?? throw new Exception("Connection cannot be null.");
        _connectionIsOpened = connectionIsOpened;
        _transaction = transaction;
    }

    public async Task<DatabaseSchema> ReadSchema(string schemaName, DatabaseServerOptions options) {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        throw new NotImplementedException();
    }

    public async Task ApplySchemaMigration(DatabaseSchemaMigration migration, DatabaseServerOptions options) {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        throw new NotImplementedException();
    }
}
