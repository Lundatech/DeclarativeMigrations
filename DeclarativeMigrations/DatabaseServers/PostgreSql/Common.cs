using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    private string GetSequenceName(DatabaseTableColumn tableColumn) {
        // PostgreSQL convention: <table_name>_<column_name>_seq
        return $"ltdmseq_{tableColumn.ParentTable.Name}__{tableColumn.Name}";
    }
    
    public override string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options) {
        return $"\"{table.ParentSchema.Name}\".\"{table.Name}\"";
    }

    public override string GetQuotedSequenceName(DatabaseSequence sequence, DatabaseServerOptions options) {
        return $"\"{sequence.ParentSchema.Name}\".\"{sequence.Name}\"";
    }

    public override string GetQuotedTableColumnName(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return $"\"{tableColumn.Name}\"";
    }

}