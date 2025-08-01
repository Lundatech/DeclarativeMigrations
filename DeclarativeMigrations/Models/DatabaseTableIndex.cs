using System;
using System.Collections.Generic;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableIndex {
    private readonly string[] _columnNames;
    
    public DatabaseTable ParentTable { get; private set; }
    public string Name { get; private set; }
    public IReadOnlyList<string> ColumnNames => _columnNames;
    
    public DatabaseTableIndex(DatabaseTable parentTable, string name, string[] columnNames) {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Index name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Index name cannot contain leading or trailing whitespace.", nameof(name));
        
        ParentTable = parentTable;
        Name = name;
        _columnNames = columnNames;
    }
}
