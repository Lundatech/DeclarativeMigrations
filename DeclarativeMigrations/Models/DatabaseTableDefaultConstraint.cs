using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableDefaultConstraint {
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }

    public DatabaseTableDefaultConstraint(DatabaseTable parentTable, string name) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Default constraint name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Default constraint name cannot contain leading or trailing whitespace.", nameof(name));
        
        ParentTable = parentTable;
        Name = name;
    }

}