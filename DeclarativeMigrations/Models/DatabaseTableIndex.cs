using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableIndex {
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }

    public DatabaseTableIndex(DatabaseTable parentTable, string name) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Index name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Index name cannot contain leading or trailing whitespace.", nameof(name));
        
        ParentTable = parentTable;
        Name = name;
    }
}
