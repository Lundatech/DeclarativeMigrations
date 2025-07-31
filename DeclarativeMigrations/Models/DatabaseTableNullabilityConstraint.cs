using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableNullabilityConstraint {
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }

    public DatabaseTableNullabilityConstraint(DatabaseTable parentTable, string name) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nullability constraint name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Nullability constraint name cannot contain leading or trailing whitespace.", nameof(name));
        
        ParentTable = parentTable;
        Name = name;
    }

}