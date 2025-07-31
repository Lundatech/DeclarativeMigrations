using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer {
    public override string GetQuotedSequenceName(DatabaseSequence sequence, DatabaseServerOptions options) {
        return $"[{sequence.ParentSchema.Name}].[{sequence.Name}]";
    }

    public override string GetQuotedTableColumnName(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return $"[{tableColumn.Name}]";
    }

    public override string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options) {
        return $"[{table.ParentSchema.Name}].[{table.Name}]";
    }
}