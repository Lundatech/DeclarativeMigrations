using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTable {
    private readonly ConcurrentDictionary<string, DatabaseTableColumn> _columns = [];
    private readonly ConcurrentDictionary<string, DatabaseTableUniqueConstraint> _uniqueConstraints = [];
    private readonly ConcurrentDictionary<string, DatabaseTableDefaultConstraint> _defaultConstraints = [];
    private readonly ConcurrentDictionary<string, DatabaseTableNullabilityConstraint> _nullabilityConstraints = [];
    private readonly ConcurrentDictionary<string, DatabaseTablePrimaryKeyConstraint> _primaryKeyConstraints = [];
    private readonly ConcurrentDictionary<string, DatabaseTableIndex> _indices = [];

    public DatabaseSchema ParentSchema { get; private set; }
    public string Name { get; private set; }
    public IReadOnlyDictionary<string, DatabaseTableColumn> Columns => _columns;
    public IReadOnlyDictionary<string, DatabaseTableUniqueConstraint> UniqueConstraints => _uniqueConstraints;
    public IReadOnlyDictionary<string, DatabaseTableDefaultConstraint> DefaultConstraints => _defaultConstraints;
    public IReadOnlyDictionary<string, DatabaseTableNullabilityConstraint> NullabilityConstraints => _nullabilityConstraints;
    public IReadOnlyDictionary<string, DatabaseTablePrimaryKeyConstraint> PrimaryKeyConstraints => _primaryKeyConstraints;
    public IReadOnlyDictionary<string, DatabaseTableIndex> Indices => _indices;

    public DatabaseTable(DatabaseSchema parentSchema, string name) {
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

    internal void AddUniqueConstraint(DatabaseTableUniqueConstraint uniqueConstraint) {
        if (uniqueConstraint == null)
            throw new ArgumentNullException(nameof(uniqueConstraint), "Unique constraint cannot be null.");
        if (uniqueConstraint.ParentTable != this)
            throw new ArgumentException("Unique constraint does not belong to this table.", nameof(uniqueConstraint));
        if (!_uniqueConstraints.TryAdd(uniqueConstraint.Name, uniqueConstraint))
            throw new ArgumentException($"Unique constraint with name '{uniqueConstraint.Name}' already exists in the table.", nameof(uniqueConstraint));
    }

    internal void AddDefaultConstraint(DatabaseTableDefaultConstraint defaultConstraint) {
        if (defaultConstraint == null)
            throw new ArgumentNullException(nameof(defaultConstraint), "Default constraint cannot be null.");
        if (defaultConstraint.ParentTable != this)
            throw new ArgumentException("Default constraint does not belong to this table.", nameof(defaultConstraint));
        if (!_defaultConstraints.TryAdd(defaultConstraint.Name, defaultConstraint))
            throw new ArgumentException($"Default constraint with name '{defaultConstraint.Name}' already exists in the table.", nameof(defaultConstraint));
    }
    
    public override string ToString() {
        return $"{ParentSchema} :: {Name}";
    }
}
