using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Lundatech.DeclarativeMigrations.Builders;
using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.DatabaseServers;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseSchema {
    private readonly ConcurrentDictionary<string, DatabaseTable> _tables = [];
    private readonly ConcurrentDictionary<string, DatabaseSequence> _sequences = [];
    
    // private readonly ConcurrentDictionary<string, DatabaseUserDefinedType> _types = [];
    // private readonly ConcurrentDictionary<string, DatabaseProcedure> _procedures = [];
    // private readonly ConcurrentDictionary<string, DatabaseFunction> _functions = [];
    // private readonly ConcurrentDictionary<string, DatabaseView> _views = [];

    public string Name { get; private set; }
    public Version SchemaOrApplicationVersion { get; private set; }
    public IReadOnlyDictionary<string, DatabaseTable> Tables => _tables;
    public IReadOnlyDictionary<string, DatabaseSequence> Sequences => _sequences;

    public DatabaseSchema(string name, Version schemaOrApplicationVersion) {
        Name = name;
        SchemaOrApplicationVersion = schemaOrApplicationVersion;
    }

    public TableBuilder<TCustomTypes, TCustomTypeProvider> AddTable<TCustomTypes, TCustomTypeProvider>(string tableName, TCustomTypeProvider customTypeProvider) where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
        //if (string.IsNullOrWhiteSpace(tableName))
        //    throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        //if (tableName.Trim() != tableName)
        //    throw new ArgumentException("Table name cannot contain leading or trailing whitespace.", nameof(tableName));
        //if (_tables.ContainsKey(tableName))
        //    throw new ArgumentException($"Table with name '{tableName}' already exists in the schema.", nameof(tableName));

        return new TableBuilder<TCustomTypes, TCustomTypeProvider>(this, tableName, customTypeProvider);
    }

    public TableBuilder<NullCustomTypes, NullCustomTypeProvider> AddStandardTable(string tableName) {
        //if (string.IsNullOrWhiteSpace(tableName))
        //    throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        //if (tableName.Trim() != tableName)
        //    throw new ArgumentException("Table name cannot contain leading or trailing whitespace.", nameof(tableName));
        //if (_tables.ContainsKey(tableName))
        //    throw new ArgumentException($"Table with name '{tableName}' already exists in the schema.", nameof(tableName));

        return new TableBuilder<NullCustomTypes, NullCustomTypeProvider>(this, tableName, new NullCustomTypeProvider());
    }

    internal void AddTable(DatabaseTable table) {
        if (table == null)
            throw new ArgumentNullException(nameof(table), "Table cannot be null.");
        if (table.ParentSchema != this)
            throw new ArgumentException("Table does not belong to this schema.", nameof(table));
        if (!_tables.TryAdd(table.Name, table))
            throw new ArgumentException($"Table with name '{table.Name}' already exists in the schema.", nameof(table));
    }

    internal void AddSequence(DatabaseSequence sequence) {
        if (sequence == null)
            throw new ArgumentNullException(nameof(sequence), "Sequence cannot be null.");
        if (sequence.ParentSchema != this)
            throw new ArgumentException("Sequence does not belong to this schema.", nameof(sequence));
        if (!_sequences.TryAdd(sequence.Name, sequence))
            throw new ArgumentException($"Sequence with name '{sequence.Name}' already exists in the schema.", nameof(sequence));
    }
    
    public DatabaseSchemaMigration GetMigrationToTargetSchema(DatabaseSchema targetSchema, DatabaseServerOptions options) {
        if (targetSchema == null)
            throw new ArgumentNullException(nameof(targetSchema), "Target schema cannot be null.");

        return new DatabaseSchemaMigration(this, targetSchema, options);
    }

    public override string ToString() {
        return $"[{Name} {SchemaOrApplicationVersion}]";
    }
}
