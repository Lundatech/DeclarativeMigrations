using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseSequence {
    public DatabaseSchema ParentSchema { get; private set; }
    public string Name { get; private set; }

    public DatabaseSequence(DatabaseSchema parentSchema, string name) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sequence name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException($"Sequence name cannot contain leading or trailing whitespace.", nameof(name));

        ParentSchema = parentSchema;
        Name = name;
    }
}