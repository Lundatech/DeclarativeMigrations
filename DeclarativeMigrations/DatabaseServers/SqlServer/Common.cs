using System.Collections.Generic;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer {
    public override string GetSequenceName(DatabaseTableColumn tableColumn) {
        var sequenceName = $"ltdmseq_{tableColumn.ParentTable.Name}__{tableColumn.Name}";
        if (sequenceName.Length > 63) {
            // PostgreSQL has a maximum identifier length of 63 characters
            sequenceName = sequenceName.Substring(0, 63);
        }
        return sequenceName;
    }

    public override string GetIndexName(DatabaseTable table, List<string> columnNames) {
        var indexName = $"ltdmidx_{table.Name}__{string.Join("__", columnNames)}";
        if (indexName.Length > 63) {
            // PostgreSQL has a maximum identifier length of 63 characters
            indexName = indexName.Substring(0, 63);
        }
        return indexName;
    }

    public override string GetQuotedSequenceName(DatabaseSequence sequence, DatabaseServerOptions options) {
        return $"[{sequence.ParentSchema.Name}].[{sequence.Name}]";
    }

    public override string GetQuotedTableColumnName(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return $"[{tableColumn.Name}]";
    }

    public override string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options) {
        return $"[{table.ParentSchema.Name}].[{table.Name}]";
    }

    public override string GetQuotedTableIndexName(DatabaseTableIndex tableIndex, bool includeSchema, DatabaseServerOptions options) {
        if (includeSchema)
            return $"[{tableIndex.ParentTable.ParentSchema.Name}].[{tableIndex.Name}]";
        return $"[{tableIndex.Name}]";
    }
}