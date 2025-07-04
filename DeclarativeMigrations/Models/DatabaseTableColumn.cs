namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableColumn {
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }
    public DatabaseType Type { get; private set; }
    public bool IsNullable { get; private set; }

    public DatabaseTableColumn(DatabaseTable parentTable, string name, DatabaseType type, bool isNullable, DatabaseTableColumnDefaultValue? defaultValue = null) {
        ParentTable = parentTable;
        Name = name;
        Type = type;
        IsNullable = isNullable;
    }
}
