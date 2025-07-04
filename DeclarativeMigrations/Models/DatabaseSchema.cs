using System;
using System.Collections.Concurrent;

using Lundatech.DeclarativeMigrations.Builders;
using Lundatech.DeclarativeMigrations.CustomTypes;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseSchema {
    private readonly ConcurrentDictionary<string, DatabaseTable> _tables = [];
    private readonly ConcurrentDictionary<string, DatabaseType> _types = [];
    private readonly ConcurrentDictionary<string, DatabaseProcedure> _procedures = [];
    private readonly ConcurrentDictionary<string, DatabaseTableContent> _tableContents = [];
    //private readonly ConcurrentDictionary<string, DatabaseFunction> _functions = [];
    //private readonly ConcurrentDictionary<string, DatabaseView> _views = [];

    public string Name { get; private set; }

    public DatabaseSchema(string name) {
        Name = name;
    }

    public TableBuilder<TCustomTypes, TCustomTypeProvider> AddTable<TCustomTypes, TCustomTypeProvider>(string tableName) where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (tableName.Trim() != tableName)
            throw new ArgumentException("Table name cannot contain leading or trailing whitespace.", nameof(tableName));
        if (_tables.ContainsKey(tableName))
            throw new ArgumentException($"Table with name '{tableName}' already exists in the schema.", nameof(tableName));

        var table = new DatabaseTable(this, tableName);
        _tables[tableName] = table;
        return new TableBuilder<TCustomTypes, TCustomTypeProvider>(this, table);
    }

    public TableBuilder<NullCustomTypes, NullCustomTypeProvider> AddStandardTable(string tableName) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (tableName.Trim() != tableName)
            throw new ArgumentException("Table name cannot contain leading or trailing whitespace.", nameof(tableName));
        if (_tables.ContainsKey(tableName))
            throw new ArgumentException($"Table with name '{tableName}' already exists in the schema.", nameof(tableName));

        var table = new DatabaseTable(this, tableName);
        _tables[tableName] = table;
        return new TableBuilder<NullCustomTypes, NullCustomTypeProvider>(this, table);
    }

    public void AddTable(DatabaseTable table) {
        if (table == null)
            throw new ArgumentNullException(nameof(table), "Table cannot be null.");
        if (table.ParentSchema != this)
            throw new ArgumentException("Table does not belong to this schema.", nameof(table));
        if (!_tables.TryAdd(table.Name, table))
            throw new ArgumentException($"Table with name '{table.Name}' already exists in the schema.", nameof(table));
    }

    public DatabaseSchemaMigration GetMigrationToTargetSchema(DatabaseSchema targetSchema, string? migrationTemporaryStorageSchemaName = null, string migrationTemporaryStorageTablePrefix = "ltdm") {
        if (targetSchema == null)
            throw new ArgumentNullException(nameof(targetSchema), "Target schema cannot be null.");

        throw new NotImplementedException();
    }
}
