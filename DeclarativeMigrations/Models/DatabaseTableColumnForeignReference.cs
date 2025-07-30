namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableColumnForeignReference {
    public DatabaseTableColumn Column { get; private set; }
    public DatabaseTable ReferencedTable { get; private set; }
    public DatabaseTableColumn ReferencedColumn { get; private set; }
    
    public DatabaseTableColumnForeignReference(DatabaseTableColumn column, DatabaseTable referencedTable, DatabaseTableColumn referencedColumn) {
        Column = column;
        ReferencedTable = referencedTable;
        ReferencedColumn = referencedColumn;
    }
}
