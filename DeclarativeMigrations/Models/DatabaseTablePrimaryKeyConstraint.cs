using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class  DatabaseTablePrimaryKeyConstraint {
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }

    public DatabaseTablePrimaryKeyConstraint(DatabaseTable parentTable, string name) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Primary key constraint name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Primary key constraint name cannot contain leading or trailing whitespace.", nameof(name));
        
        ParentTable = parentTable;
        Name = name;
    }

}
