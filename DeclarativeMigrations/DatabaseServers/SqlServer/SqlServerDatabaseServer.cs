using System;
using System.Data;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer : DatabaseServerBase {
    private readonly SqlConnection _connection;
    private bool _connectionIsOpened;
    private readonly SqlTransaction? _transaction;

    public SqlServerDatabaseServer(SqlConnection connection, bool connectionIsOpened, SqlTransaction? transaction = null) {
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
