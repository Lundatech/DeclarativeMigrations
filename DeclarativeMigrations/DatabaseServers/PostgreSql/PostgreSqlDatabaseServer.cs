using System;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer : IDatabaseServer {
    private readonly NpgsqlConnection _connection;
    private bool _connectionIsOpened;
    private readonly NpgsqlTransaction? _transaction;

    public PostgreSqlDatabaseServer(NpgsqlConnection connection, bool connectionIsOpened, NpgsqlTransaction? transaction = null) {
        _connection = connection ?? throw new Exception("Connection cannot be null.");
        _connectionIsOpened = connectionIsOpened;
        _transaction = transaction;
    }

    //public async IAsyncEnumerable<DatabaseSchema> ReadAllSchemas() {
    //    if (!_connectionIsOpened) {
    //        await _connection.OpenAsync();
    //        _connectionIsOpened = true;
    //    }

    //    var schemaNames = await ReadSchemaNamesFromServer();
    //    foreach (var schemaName in schemaNames) {
    //        yield return await ReadSchemaFromServer(schemaName);
    //    }
    //}

    public async Task<DatabaseSchema> ReadSchema(string schemaName, DatabaseServerOptions options) {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        return await ReadSchemaFromServer(schemaName, options);
    }

    public async Task ApplySchemaMigration(DatabaseSchemaMigration migration, DatabaseServerOptions options) {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }

        throw new NotImplementedException();
    }
}
