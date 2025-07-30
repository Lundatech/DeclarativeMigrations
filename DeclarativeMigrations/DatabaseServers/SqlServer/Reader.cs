using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer {
    public override Task<DatabaseSchema> ReadSchema(string schemaName, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }
}