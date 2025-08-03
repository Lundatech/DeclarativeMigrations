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

    public enum MigrationType {
        Upgrade,
        Downgrade,
        SameVersion
    }

    public MigrationType Type { get; }

    public class SchemaDifference {
        private bool _handled = false;

        public enum ObjectType {
            Sequence,
            Table,
            TableColumn,
            TableIndex,
        }

        public enum DifferenceType {
            Added,
            Dropped,
            Altered
        }

        public enum PropertyType {
            Type,
            Nullability,
            DefaultValue,
            PrimaryKey,
            Unique,
            ForeignReference,
            Columns,
        }

        public ObjectType Object { get; }
        public DifferenceType Type { get; }
        public PropertyType? Property { get; }

        public DatabaseSequence? DatabaseSequence { get; private set; }
        public DatabaseSequence? TargetSequence { get; private set; }

        public DatabaseTable? DatabaseTable { get; private set; }
        public DatabaseTable? TargetTable { get; private set; }

        public DatabaseTableColumn? DatabaseTableColumn { get; private set; }
        public DatabaseTableColumn? TargetTableColumn { get; private set; }

        public DatabaseTableIndex? DatabaseTableIndex { get; private set; }
        public DatabaseTableIndex? TargetTableIndex { get; private set; }

        public bool IsHandled => _handled;

        //public DatabaseType? TypeDefinition => Object == ObjectType.Type ? _parentMigration.TargetSchema.Types[ObjectName] : null;
        //public DatabaseProcedure? Procedure => Object == ObjectType.Procedure ? _parentMigration.TargetSchema.Procedures[ObjectName] : null;
        //public DatabaseTableContent? TableContent => Object == ObjectType.TableContent ? _parentMigration.TargetSchema.TableContents[ObjectName] : null;

        private SchemaDifference(ObjectType objectType, DifferenceType differenceType, PropertyType? propertyType) {
            Object = objectType;
            Type = differenceType;
            Property = propertyType;
        }

        public static SchemaDifference CreateSequenceDifference(DifferenceType differenceType, DatabaseSequence? databaseSequence,
            DatabaseSequence? targetSequence) {
            return new SchemaDifference(ObjectType.Sequence, differenceType, null) {
                DatabaseSequence = databaseSequence,
                TargetSequence = targetSequence
            };
        }

        public static SchemaDifference CreateTableDifference(DifferenceType differenceType, PropertyType? propertyType, DatabaseTable? databaseTable,
            DatabaseTable? targetTable) {
            return new SchemaDifference(ObjectType.Table, differenceType, propertyType) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable
            };
        }

        public static SchemaDifference CreateTableIndexDifference(DifferenceType differenceType, DatabaseTable? databaseTable,
            DatabaseTableIndex? databaseTableIndex, DatabaseTable? targetTable, DatabaseTableIndex? targetTableIndex) {
            return new SchemaDifference(ObjectType.TableIndex, differenceType, null) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable,
                DatabaseTableIndex = databaseTableIndex,
                TargetTableIndex = targetTableIndex
            };
        }

        public static SchemaDifference CreateTableColumnDifference(DifferenceType differenceType, DatabaseTable databaseTable,
            DatabaseTableColumn? databaseTableColumn, DatabaseTable targetTable, DatabaseTableColumn? targetTableColumn) {
            return new SchemaDifference(ObjectType.TableColumn, differenceType, null) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable,
                DatabaseTableColumn = databaseTableColumn,
                TargetTableColumn = targetTableColumn
            };
        }

        public static SchemaDifference CreateColumnDifference(PropertyType propertyType, DatabaseTable databaseTable, DatabaseTableColumn databaseTableColumn,
            DatabaseTable targetTable, DatabaseTableColumn targetTableColumn) {
            return new SchemaDifference(ObjectType.TableColumn, DifferenceType.Altered, propertyType) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable,
                DatabaseTableColumn = databaseTableColumn,
                TargetTableColumn = targetTableColumn
            };
        }

        public static SchemaDifference CreateIndexDifference(PropertyType propertyType, DatabaseTable databaseTable, DatabaseTableIndex databaseTableIndex,
            DatabaseTable targetTable, DatabaseTableIndex targetTableIndex) {
            return new SchemaDifference(ObjectType.TableIndex, DifferenceType.Altered, propertyType) {
                DatabaseTable = databaseTable,
                TargetTable = targetTable,
                DatabaseTableIndex = databaseTableIndex,
                TargetTableIndex = targetTableIndex
            };
        }

        internal void SetHandled() {
            _handled = true;
        }
    };

    public DatabaseSchemaMigration(DatabaseSchema databaseSchema, DatabaseSchema targetSchema, DatabaseServerOptions options) {
        DatabaseSchema = databaseSchema;
        TargetSchema = targetSchema;

        Differences = GetDifferences().ToImmutableList();

        if (targetSchema.SchemaOrApplicationVersion > databaseSchema.SchemaOrApplicationVersion) {
            Type = MigrationType.Upgrade;
        }
        else if (targetSchema.SchemaOrApplicationVersion < databaseSchema.SchemaOrApplicationVersion) {
            Type = MigrationType.Downgrade;
        }
        else {
            // FIXME: re-enable this check once things are more stable
            if (Differences.Any()) {
                if (options.SameVersionDifferencesHandling == DatabaseServerOptions.SameVersionDifferencesHandlingType.Error)
                    throw new InvalidOperationException("Schema or application versions are the same, but there are differences in the actual schemas.");
                else if (options.SameVersionDifferencesHandling == DatabaseServerOptions.SameVersionDifferencesHandlingType.TreatAsUpgrade)
                    Type = MigrationType.Upgrade; // or Downgrade, depending on the context
            }
            else
                Type = MigrationType.SameVersion;
        }
    }

    public bool IsEmpty() => !Differences.Any();

    private List<SchemaDifference> GetDifferences() {
        var differences = new List<SchemaDifference>();

        differences.AddRange(GetSequenceDifferences());
        differences.AddRange(GetTableDifferences());
        //differences.AddRange(GetTypeDifferences());
        //differences.AddRange(GetProcedureDifferences());
        //differences.AddRange(GetTableContentDifferences());

        return differences;
    }

    private List<SchemaDifference> GetSequenceDifferences() {
        var differences = new List<SchemaDifference>();

        foreach (var targetSequence in TargetSchema.Sequences) {
            if (!DatabaseSchema.Sequences.ContainsKey(targetSequence.Key)) {
                differences.Add(SchemaDifference.CreateSequenceDifference(SchemaDifference.DifferenceType.Added, null, targetSequence.Value));
            }
        }

        foreach (var databaseSequence in DatabaseSchema.Sequences) {
            if (!TargetSchema.Sequences.ContainsKey(databaseSequence.Key)) {
                differences.Add(SchemaDifference.CreateSequenceDifference(SchemaDifference.DifferenceType.Dropped, databaseSequence.Value, null));
            }
        }

        // no altered needed

        return differences;
    }

    private List<SchemaDifference> GetTableDifferences() {
        var differences = new List<SchemaDifference>();

        foreach (var targetTable in TargetSchema.GetOrderedTables()) {
            if (!DatabaseSchema.Tables.ContainsKey(targetTable.Name)) {
                differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Added, null, null, targetTable));

                // also add all indexes
                foreach (var index in targetTable.Indexes) {
                    differences.Add(SchemaDifference.CreateTableIndexDifference(SchemaDifference.DifferenceType.Added, null, null, targetTable, index.Value));
                }
            }
        }

        foreach (var databaseTable in DatabaseSchema.Tables) {
            if (!TargetSchema.Tables.ContainsKey(databaseTable.Key)) {
                differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Dropped, null, databaseTable.Value, null));

                // also drop all indexes
                foreach (var index in databaseTable.Value.Indexes) {
                    differences.Add(SchemaDifference.CreateTableIndexDifference(SchemaDifference.DifferenceType.Dropped, databaseTable.Value, index.Value, null,
                        null));
                }
            }
        }

        foreach (var databaseTable in DatabaseSchema.Tables) {
            if (TargetSchema.Tables.TryGetValue(databaseTable.Key, out var targetTable)) {
                differences.AddRange(GetColumnDifferencesForTable(databaseTable.Value, targetTable));
                differences.AddRange(GetTableDifferences(databaseTable.Value, targetTable));
                differences.AddRange(GetIndexDifferencesForTable(databaseTable.Value, targetTable));
            }
        }

        return differences;
    }

    private List<SchemaDifference> GetColumnDifferencesForTable(DatabaseTable databaseTable, DatabaseTable targetTable) {
        var differences = new List<SchemaDifference>();

        foreach (var targetColumn in targetTable.Columns) {
            if (!databaseTable.Columns.ContainsKey(targetColumn.Key)) {
                differences.Add(SchemaDifference.CreateTableColumnDifference(SchemaDifference.DifferenceType.Added, databaseTable, null, targetTable,
                    targetColumn.Value));
            }
        }

        foreach (var databaseColumn in databaseTable.Columns) {
            if (!targetTable.Columns.ContainsKey(databaseColumn.Key)) {
                differences.Add(SchemaDifference.CreateTableColumnDifference(SchemaDifference.DifferenceType.Dropped, databaseTable, databaseColumn.Value,
                    targetTable, null));
            }
        }

        foreach (var databaseColumn in databaseTable.Columns) {
            if (targetTable.Columns.TryGetValue(databaseColumn.Key, out var targetColumn)) {
                differences.AddRange(GetDifferencesForColumn(databaseTable, databaseColumn.Value, targetTable, targetColumn));
            }
        }

        return differences;
    }

    private List<SchemaDifference> GetIndexDifferencesForTable(DatabaseTable databaseTable, DatabaseTable targetTable) {
        var differences = new List<SchemaDifference>();

        foreach (var targetIndex in targetTable.Indexes) {
            if (!databaseTable.Indexes.ContainsKey(targetIndex.Key)) {
                differences.Add(SchemaDifference.CreateTableIndexDifference(SchemaDifference.DifferenceType.Added, databaseTable, null, targetTable,
                    targetIndex.Value));
            }
        }

        foreach (var databaseIndex in databaseTable.Indexes) {
            if (!targetTable.Indexes.ContainsKey(databaseIndex.Key)) {
                differences.Add(SchemaDifference.CreateTableIndexDifference(SchemaDifference.DifferenceType.Dropped, databaseTable, databaseIndex.Value,
                    targetTable, null));
            }
        }

        foreach (var databaseIndex in databaseTable.Indexes) {
            if (targetTable.Indexes.TryGetValue(databaseIndex.Key, out var targetIndex)) {
                differences.AddRange(GetDifferencesForIndex(databaseTable, databaseIndex.Value, targetTable, targetIndex));
            }
        }

        return differences;
    }

    private List<SchemaDifference> GetTableDifferences(DatabaseTable databaseTable, DatabaseTable targetTable) {
        var differences = new List<SchemaDifference>();

        // check if the primary key columns have changed
        var databasePrimaryKeyColumns = databaseTable.Columns.Values.Where(c => c.IsPrimaryKey).Select(c => c.Name).Order().ToList();
        var targetPrimaryKeyColumns = targetTable.Columns.Values.Where(c => c.IsPrimaryKey).Select(c => c.Name).Order().ToList();
        if (!databasePrimaryKeyColumns.SequenceEqual(targetPrimaryKeyColumns))
            differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Altered, SchemaDifference.PropertyType.PrimaryKey,
                databaseTable, targetTable));

        // check if the unique columns have changed
        var databaseUniqueColumns = databaseTable.Columns.Values.Where(c => c.IsUnique).Select(c => c.Name).Order().ToList();
        var targetUniqueColumns = targetTable.Columns.Values.Where(c => c.IsUnique).Select(c => c.Name).Order().ToList();
        if (!databaseUniqueColumns.SequenceEqual(targetUniqueColumns))
            differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Altered, SchemaDifference.PropertyType.Unique, databaseTable,
                targetTable));

        return differences;
    }

    private List<SchemaDifference> GetDifferencesForColumn(DatabaseTable databaseTable, DatabaseTableColumn databaseTableColumn, DatabaseTable targetTable,
        DatabaseTableColumn targetTableColumn) {
        var differences = new List<SchemaDifference>();

        if (databaseTableColumn.Type != targetTableColumn.Type)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.Type, databaseTable, databaseTableColumn, targetTable,
                targetTableColumn));
        if (databaseTableColumn.IsNullable != targetTableColumn.IsNullable)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.Nullability, databaseTable, databaseTableColumn, targetTable,
                targetTableColumn));
        if (databaseTableColumn.IsPrimaryKey != targetTableColumn.IsPrimaryKey)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.PrimaryKey, databaseTable, databaseTableColumn, targetTable,
                targetTableColumn));
        if (databaseTableColumn.IsUnique != targetTableColumn.IsUnique)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.Unique, databaseTable, databaseTableColumn, targetTable,
                targetTableColumn));
        if (databaseTableColumn.DefaultValue != targetTableColumn.DefaultValue)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.DefaultValue, databaseTable, databaseTableColumn, targetTable,
                targetTableColumn));
        if (databaseTableColumn.ForeignReference != targetTableColumn.ForeignReference)
            differences.Add(SchemaDifference.CreateColumnDifference(SchemaDifference.PropertyType.ForeignReference, databaseTable, databaseTableColumn,
                targetTable, targetTableColumn));

        return differences;
    }

    private List<SchemaDifference> GetDifferencesForIndex(DatabaseTable databaseTable, DatabaseTableIndex databaseTableIndex, DatabaseTable targetTable,
        DatabaseTableIndex targetTableIndex) {
        var differences = new List<SchemaDifference>();

        if (!databaseTableIndex.ColumnNames.Order().SequenceEqual(targetTableIndex.ColumnNames.Order()))
            differences.Add(SchemaDifference.CreateIndexDifference(SchemaDifference.PropertyType.Columns, databaseTable, databaseTableIndex, targetTable,
                targetTableIndex));

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