using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer {
    public override Task UpdateSchemaVersion(DatabaseSchema schemaMigration, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }

    public override Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }
  
    public override string GetTableColumnDataTypeScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }
    
    public override List<string> GetTableColumnExtraCreateScripts(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }
    
    public override List<string> GetTableExtraCreateScripts(DatabaseTable table, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }
    
    public override async Task ExecuteScript(string script, DatabaseServerOptions options) {
        await using var command = new SqlCommand(script, _connection, _transaction);
        await command.ExecuteNonQueryAsync();

    }
}