using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer {
    public override async Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options) {
        throw new System.NotImplementedException();
    }

    public override string GetQuotedTableColumnName(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return $"[{tableColumn.Name}]";
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

    public override string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options) {
        return $"[{table.ParentSchema.Name}].[{table.Name}]";
    }
    
    public override async Task ExecuteScript(string script, DatabaseServerOptions options) {
        await EnsureConnectionIsOpen();
        await using var command = new SqlCommand(script, _connection, _transaction);
        await command.ExecuteNonQueryAsync();

    }
}