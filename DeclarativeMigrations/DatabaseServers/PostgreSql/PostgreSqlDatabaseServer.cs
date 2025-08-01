using System;
using System.Threading.Tasks;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer : DatabaseServerBase {
    private readonly NpgsqlConnection _connection;
    private bool _connectionIsOpened;
    private readonly NpgsqlTransaction? _transaction;

    public PostgreSqlDatabaseServer(NpgsqlConnection connection, bool connectionIsOpened, NpgsqlTransaction? transaction = null) {
        _connection = connection ?? throw new Exception("Connection cannot be null.");
        _connectionIsOpened = connectionIsOpened;
        _transaction = transaction;
    }

    public override async Task EnsureConnectionIsOpen() {
        if (!_connectionIsOpened) {
            await _connection.OpenAsync();
            _connectionIsOpened = true;
        }
    }
}
