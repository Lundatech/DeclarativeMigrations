using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Lundatech.DeclarativeMigrations.DatabaseServers;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseSchemaMigration {
    public DatabaseSchema DatabaseSchema { get; }
    public DatabaseSchema TargetSchema { get; }
    public ImmutableList<SchemaDifference> Differences { get; private set; }
    // public ImmutableList<string> MigrationSteps { get; private set; }

    public enum MigrationType {
        Upgrade,
        Downgrade,
        Identical
    }
    
    public MigrationType Type { get; }

    public class SchemaDifference {
        public enum ObjectType {
            Table,
            Column,
            //Type,
            //Procedure,
            //TableContent,
            //Function,
            //View
        }

        public enum DifferenceType {
            Added,
            Dropped,
            Altered
        }

        public enum PropertyType {
            // Name,
            Type,
            // Length,
            // Precision,
            // Scale,
            Nullability,
            DefaultValue,
            PrimaryKey,
            Unique,
            ForeignReference,
            // ReferencedTable,
            // ReferencedColumn,
            // ReferenceUpdateAction,
            // ReferenceDeleteAction,
            // UniqueConstraint,
        }

        public ObjectType Object { get; }
        public DifferenceType Type { get; }
        public PropertyType? Property { get; }

        public DatabaseTable? DatabaseTable { get; private set; }
        public DatabaseTable? TargetTable { get; private set; }
        
        public DatabaseTableColumn? DatabaseTableColumn { get; private set; }
        public DatabaseTableColumn? TargetTableColumn { get; private set; }
        
        
        //public DatabaseType? TypeDefinition => Object == ObjectType.Type ? _parentMigration.TargetSchema.Types[ObjectName] : null;
        //public DatabaseProcedure? Procedure => Object == ObjectType.Procedure ? _parentMigration.TargetSchema.Procedures[ObjectName] : null;
        //public DatabaseTableContent? TableContent => Object == ObjectType.TableContent ? _parentMigration.TargetSchema.TableContents[ObjectName] : null;

        private SchemaDifference(ObjectType objectType, DifferenceType differenceType, PropertyType? propertyType) {
            Object = objectType;
            Type = differenceType;
            Property = propertyType;
        }

        public static SchemaDifference CreateTableDifference(DifferenceType differenceType, DatabaseTable? databaseTable, DatabaseTable? targetTable) {
            return new SchemaDifference(ObjectType.Table, differenceType, null) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable
            };
        }

        public static SchemaDifference CreateTableColumnDifference(DifferenceType differenceType, DatabaseTable databaseTable, DatabaseTableColumn? databaseTableColumn, DatabaseTable targetTable, DatabaseTableColumn? targetTableColumn) {
            return new SchemaDifference(ObjectType.Column, differenceType, null) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable,
                DatabaseTableColumn = databaseTableColumn,
                TargetTableColumn = targetTableColumn
            };
        }

        public static SchemaDifference CreateColumnDifference(PropertyType propertyType, DatabaseTable databaseTable, DatabaseTableColumn databaseTableColumn, DatabaseTable targetTable, DatabaseTableColumn targetTableColumn) {
            return new SchemaDifference(ObjectType.Column, DifferenceType.Altered, propertyType) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable,
                DatabaseTableColumn = databaseTableColumn,
                TargetTableColumn = targetTableColumn
            };
        }
    };

    public DatabaseSchemaMigration(DatabaseSchema databaseSchema, DatabaseSchema targetSchema, DatabaseServerOptions options) {
        DatabaseSchema = databaseSchema;
        TargetSchema = targetSchema;

        Differences = GetDifferences().ToImmutableList();
        if (targetSchema.SchemaOrApplicationVersion > databaseSchema.SchemaOrApplicationVersion) {
            Type = MigrationType.Upgrade;
            // MigrationSteps = BuildUpgradeMigration(Differences).ToImmutableList();
        }
        else if (targetSchema.SchemaOrApplicationVersion < databaseSchema.SchemaOrApplicationVersion) {
            Type = MigrationType.Downgrade;
            // MigrationSteps = BuildDowngradeMigration(Differences).ToImmutableList();
        }
        else {
            if (Differences.Any())
                throw new InvalidOperationException("Schema or application versions are the same, but there are differences in the actual schemas.");

            Type = MigrationType.Identical;
            // MigrationSteps = ImmutableList<string>.Empty; // No migration needed, schemas are already the same
        }
    }

    public bool IsEmpty() => !Differences.Any();

    private List<SchemaDifference> GetDifferences() {
        var differences = new List<SchemaDifference>();

        differences.AddRange(GetTableDifferences());
        //differences.AddRange(GetTypeDifferences());
        //differences.AddRange(GetProcedureDifferences());
        //differences.AddRange(GetTableContentDifferences());

        return differences;
    }

    private List<SchemaDifference> GetTableDifferences() {
        var differences = new List<SchemaDifference>();

        foreach (var targetTable in TargetSchema.Tables) {
            if (!DatabaseSchema.Tables.ContainsKey(targetTable.Key)) {
                differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Added, null, targetTable.Value));
            }
        }

        foreach (var databaseTable in DatabaseSchema.Tables) {
            if (!TargetSchema.Tables.ContainsKey(databaseTable.Key)) {
                differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Dropped, databaseTable.Value, null));
            }
        }

        foreach (var databaseTable in DatabaseSchema.Tables) {
            if (TargetSchema.Tables.TryGetValue(databaseTable.Key, out var targetTable)) {
                differences.AddRange(GetColumnDifferencesForTable(databaseTable.Value, targetTable));
                differences.AddRange(GetTableDifferences(databaseTable.Value, targetTable));
            }
        }

        return differences;
    }

    private List<SchemaDifference> GetColumnDifferencesForTable(DatabaseTable databaseTable, DatabaseTable targetTable) {
        var differences = new List<SchemaDifference>();

        foreach (var targetColumn in targetTable.Columns) {
            if (!databaseTable.Columns.ContainsKey(targetColumn.Key)) {
                differences.Add(SchemaDifference.CreateTableColumnDifference(SchemaDifference.DifferenceType.Added, databaseTable, null, targetTable, targetColumn.Value));
            }
        }

        foreach (var databaseColumn in databaseTable.Columns) {
            if (!targetTable.Columns.ContainsKey(databaseColumn.Key)) {
                differences.Add(SchemaDifference.CreateTableColumnDifference(SchemaDifference.DifferenceType.Dropped, databaseTable, databaseColumn.Value, targetTable, null));
            }
        }

        foreach (var databaseColumn in databaseTable.Columns) {
            if (targetTable.Columns.TryGetValue(databaseColumn.Key, out var targetColumn)) {
                differences.AddRange(GetDifferencesForColumn(databaseTable, databaseColumn.Value, targetTable, targetColumn));
            }
        }
        
        return differences;
    }

    private List<SchemaDifference> GetTableDifferences(DatabaseTable databaseTable, DatabaseTable targetTable) {
        var differences = new List<SchemaDifference>();

        // if (databaseTable.Indices != targetTable.Indices)
        //     ...
        
        return differences;
    }
    
    private List<SchemaDifference> GetDifferencesForColumn(DatabaseTable databaseTable, DatabaseTableColumn databaseTableColumn, DatabaseTable targetTable, DatabaseTableColumn targetTableColumn) {
        var differences = new List<SchemaDifference>();

        if (databaseTableColumn.Type != targetTableColumn.Type) 
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.Type, databaseTable, databaseTableColumn, targetTable, targetTableColumn));
        if (databaseTableColumn.IsNullable != targetTableColumn.IsNullable) 
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.Nullability, databaseTable, databaseTableColumn, targetTable, targetTableColumn));
        if (databaseTableColumn.IsPrimaryKey != targetTableColumn.IsPrimaryKey)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.PrimaryKey, databaseTable, databaseTableColumn, targetTable, targetTableColumn));
        if (databaseTableColumn.IsUnique != targetTableColumn.IsUnique)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.Unique, databaseTable, databaseTableColumn, targetTable, targetTableColumn));
        if (databaseTableColumn.DefaultValue != targetTableColumn.DefaultValue)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.DefaultValue, databaseTable, databaseTableColumn, targetTable, targetTableColumn));
        if (databaseTableColumn.ForeignReference != targetTableColumn.ForeignReference)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.ForeignReference, databaseTable, databaseTableColumn, targetTable, targetTableColumn));
        
        return differences;
    }
    
    //private List<SchemaDifference> GetTypeDifferences() {
    //    var differences = new List<SchemaDifference>();
    //    // Logic to compare types between DatabaseSchema and TargetSchema
    //    // ...
    //    return differences;
    //}

    //private List<SchemaDifference> GetProcedureDifferences() {
    //    var differences = new List<SchemaDifference>();
    //    // Logic to compare procedures between DatabaseSchema and TargetSchema
    //    // ...
    //    return differences;
    //}

    //private List<SchemaDifference> GetTableContentDifferences() {
    //    var differences = new List<SchemaDifference>();
    //    // Logic to compare table contents between DatabaseSchema and TargetSchema
    //    // ...
    //    return differences;
    //}

    // private List<string> BuildUpgradeMigration(ImmutableList<SchemaDifference> differences) {
    //     // first drop all procedures, functions and views
    //
    //     // get all table migration scripts
    //
    //     // lastly, create all procedures, functions and views
    //
    //     var migrationSteps = new List<string>();
    //     // Logic to build upgrade migration steps based on differences
    //     // ...
    //     return migrationSteps;
    // }
    //
    // private List<string> BuildDowngradeMigration(ImmutableList<SchemaDifference> differences) {
    //     var migrationSteps = new List<string>();
    //     // Logic to build downgrade migration steps based on differences
    //     // ...
    //     return migrationSteps;
    // }
}
