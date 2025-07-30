using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableColumn {
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }
    public DatabaseType Type { get; private set; }
    public bool IsNullable { get; private set; }
    public bool IsPrimaryKey { get; private set; }
    public bool IsUnique { get; private set; }
    public DatabaseTableColumnDefaultValue? DefaultValue { get; private set; }
    public DatabaseTableColumnForeignReference? ForeignReference { get; private set; }

    public DatabaseTableColumn(DatabaseTable parentTable, string name, DatabaseType type, bool isNullable, bool isPrimaryKey, bool isUnique, DatabaseTableColumnDefaultValue? defaultValue, DatabaseTableColumnForeignReference? foreignReference) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Column name cannot contain leading or trailing whitespace.", nameof(name));
        if (type == null)
            throw new ArgumentNullException(nameof(type), "Column type cannot be null.");

        ParentTable = parentTable;
        Name = name;
        Type = type;
        IsNullable = isNullable;
        IsPrimaryKey = isPrimaryKey;
        IsUnique = isUnique;
        DefaultValue = defaultValue;
        ForeignReference = foreignReference;
    }
    
    public override string ToString() {
        return $"{ParentTable}.{Name} {Type}{(IsNullable ? "?" : string.Empty)}{(IsPrimaryKey ? " PK" : string.Empty)}";
    }
}
