using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTable {
    private ConcurrentDictionary<string, DatabaseTableColumn> _columns = [];
    //private DatabaseTablePrimaryKey _primaryKey = null;
    private List<DatabaseTableIndex> _indices = [];
    private List<DatabaseTableUniqueConstraint> _uniqueConstraints = [];
    private HashSet<string> _tableDependencies = [];

    public DatabaseSchema ParentSchema { get; private set; }
    public string Name { get; private set; }
    public IReadOnlyDictionary<string, DatabaseTableColumn> Columns => _columns;

    public DatabaseTable(DatabaseSchema parentSchema, string name) {
        if (parentSchema == null)
            throw new ArgumentNullException(nameof(parentSchema), "Parent schema cannot be null.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(name));
        if (name.Trim() != name)
            throw new ArgumentException("Table name cannot contain leading or trailing whitespace.", nameof(name));

        ParentSchema = parentSchema;
        Name = name;
    }

    internal void AddColumn(DatabaseTableColumn column) {
        if (column == null)
            throw new ArgumentNullException(nameof(column), "Column cannot be null.");
        if (column.ParentTable != this)
            throw new ArgumentException("Column does not belong to this table.", nameof(column));
        if (!_columns.TryAdd(column.Name, column))
            throw new ArgumentException($"Column with name '{column.Name}' already exists in the table.", nameof(column));
    }
    
    public override string ToString() {
        return $"{ParentSchema.Name} -> {Name}";
    }
}
