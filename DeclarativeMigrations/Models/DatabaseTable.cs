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
    private readonly ConcurrentDictionary<string, DatabaseTableIndex> _indexes = [];

    public DatabaseSchema ParentSchema { get; private set; }
    public string Name { get; private set; }
    public IReadOnlyDictionary<string, DatabaseTableColumn> Columns => _columns;
    public IReadOnlyDictionary<string, DatabaseTableUniqueConstraint> UniqueConstraints => _uniqueConstraints;
    public IReadOnlyDictionary<string, DatabaseTableDefaultConstraint> DefaultConstraints => _defaultConstraints;
    public IReadOnlyDictionary<string, DatabaseTableNullabilityConstraint> NullabilityConstraints => _nullabilityConstraints;
    public IReadOnlyDictionary<string, DatabaseTablePrimaryKeyConstraint> PrimaryKeyConstraints => _primaryKeyConstraints;
    public IReadOnlyDictionary<string, DatabaseTableIndex> Indexes => _indexes;

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

    internal void AddNullabilityConstraint(DatabaseTableNullabilityConstraint nullabilityConstraint) {
        if (nullabilityConstraint == null)
            throw new ArgumentNullException(nameof(nullabilityConstraint), "Nullability constraint cannot be null.");
        if (nullabilityConstraint.ParentTable != this)
            throw new ArgumentException("Nullability constraint does not belong to this table.", nameof(nullabilityConstraint));
        if (!_nullabilityConstraints.TryAdd(nullabilityConstraint.Name, nullabilityConstraint))
            throw new ArgumentException($"Nullability constraint with name '{nullabilityConstraint.Name}' already exists in the table.", nameof(nullabilityConstraint));
    }
    
    internal void AddPrimaryKeyConstraint(DatabaseTablePrimaryKeyConstraint primaryKeyConstraint) {
        if (primaryKeyConstraint == null)
            throw new ArgumentNullException(nameof(primaryKeyConstraint), "Primary key constraint cannot be null.");
        if (primaryKeyConstraint.ParentTable != this)
            throw new ArgumentException("Primary key constraint does not belong to this table.", nameof(primaryKeyConstraint));
        if (!_primaryKeyConstraints.TryAdd(primaryKeyConstraint.Name, primaryKeyConstraint))
            throw new ArgumentException($"Primary key constraint with name '{primaryKeyConstraint.Name}' already exists in the table.", nameof(primaryKeyConstraint));
    }
    
    internal void AddIndex(DatabaseTableIndex index) {
        if (index == null)
            throw new ArgumentNullException(nameof(index), "Index cannot be null.");
        if (index.ParentTable != this)
            throw new ArgumentException("Index does not belong to this table.", nameof(index));
        if (!_indexes.TryAdd(index.Name, index))
            throw new ArgumentException($"Index with name '{index.Name}' already exists in the table.", nameof(index));
    }

    public List<string> GetTableReferences() {
        return _columns.Values
            .Where(x => x.ForeignReference != null)
            .Select(x => x.ForeignReference!.ForeignTableName)
            .Distinct()
            .ToList();
    }

    public override string ToString() {
        return $"{ParentSchema} :: {Name}";
    }
}
