using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    public override void TableColumnBuilderHook(DatabaseTableColumn tableColumn) {
        // if the column is a serial type, we need to ensure that a sequence is created
        if (tableColumn.Type.Type == DatabaseType.Standard.SerialInteger32 || tableColumn.Type.Type == DatabaseType.Standard.SerialInteger64) {
            var sequenceName = GetSequenceName(tableColumn);
            var sequence = new DatabaseSequence(tableColumn.ParentTable.ParentSchema, sequenceName);
            tableColumn.ParentTable.ParentSchema.AddSequence(sequence);
        }
    }
}