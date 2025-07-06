using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseSchemaMigration {
    public DatabaseSchema DatabaseSchema { get; }
    public DatabaseSchema TargetSchema { get; }
    public ImmutableList<SchemaDifference> Differences { get; private set; }
    public ImmutableList<string> MigrationSteps { get; private set; }

    //private enum MigrationType {
    //    Upgrade,
    //    Downgrade,
    //    CheckSame,
    //}

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
            Removed,
            Modified
        }

        public enum PropertyType {
            Name,
            Type,
            Length,
            Precision,
            Scale,
            IsNullable,
            DefaultValue,
            IsPrimaryKey,
            ReferencedTable,
            ReferencedColumn,
            ReferenceUpdateAction,
            ReferenceDeleteAction,
            UniqueConstraint,
        }

        public ObjectType Object { get; }
        public DifferenceType Type { get; }
        public PropertyType? Property { get; }

        public DatabaseTable? Table { get; private set; }
        //public DatabaseTableColumn? Column { get; private set; }
        //public DatabaseType? TypeDefinition => Object == ObjectType.Type ? _parentMigration.TargetSchema.Types[ObjectName] : null;
        //public DatabaseProcedure? Procedure => Object == ObjectType.Procedure ? _parentMigration.TargetSchema.Procedures[ObjectName] : null;
        //public DatabaseTableContent? TableContent => Object == ObjectType.TableContent ? _parentMigration.TargetSchema.TableContents[ObjectName] : null;

        private SchemaDifference(ObjectType objectType, DifferenceType differenceType, PropertyType? propertyType) {
            Object = objectType;
            Type = differenceType;
            Property = propertyType;
        }

        public static SchemaDifference CreateTableDifference(DifferenceType differenceType, DatabaseTable? table) {
            return new SchemaDifference(ObjectType.Table, differenceType, null) {
                Table = table
            };
        }
    };

    public DatabaseSchemaMigration(DatabaseSchema databaseSchema, DatabaseSchema targetSchema, string? migrationTemporaryStorageSchemaName, string migrationTemporaryStorageTablePrefix) {
        DatabaseSchema = databaseSchema;
        TargetSchema = targetSchema;

        Differences = GetDifferences().ToImmutableList();
        if (targetSchema.SchemaOrApplicationVersion > databaseSchema.SchemaOrApplicationVersion) {
            MigrationSteps = BuildUpgradeMigration(Differences).ToImmutableList();
        }
        else if (targetSchema.SchemaOrApplicationVersion < databaseSchema.SchemaOrApplicationVersion) {
            MigrationSteps = BuildDowngradeMigration(Differences).ToImmutableList();
        }
        else {
            if (Differences.Any())
                throw new InvalidOperationException("Schema or application versions are the same, but there are differences in the actual schemas.");

            MigrationSteps = ImmutableList<string>.Empty; // No migration needed, schemas are already the same
        }
    }

    public bool IsEmpty() => !Differences.Any() && !MigrationSteps.Any();

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

        foreach (var table in TargetSchema.Tables) {
            if (!DatabaseSchema.Tables.ContainsKey(table.Key)) {
                differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Added, table.Value));
            }
        }

        foreach (var table in DatabaseSchema.Tables) {
            if (!TargetSchema.Tables.ContainsKey(table.Key)) {
                differences.Add(SchemaDifference.CreateTableDifference(SchemaDifference.DifferenceType.Removed, table.Value));
            }
        }

        foreach (var table in DatabaseSchema.Tables) {
            if (TargetSchema.Tables.TryGetValue(table.Key, out var targetTable)) {
                differences.AddRange(GetColumnDifferencesForTable(table.Value, targetTable));
            }
        }

        return differences;
    }

    private List<SchemaDifference> GetColumnDifferencesForTable(DatabaseTable databaseTable, DatabaseTable targetTable) {
        var differences = new List<SchemaDifference>();
        // Logic to compare columns between DatabaseSchema and TargetSchema
        // ...
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

    private List<string> BuildUpgradeMigration(ImmutableList<SchemaDifference> differences) {
        var migrationSteps = new List<string>();
        // Logic to build upgrade migration steps based on differences
        // ...
        return migrationSteps;
    }

    private List<string> BuildDowngradeMigration(ImmutableList<SchemaDifference> differences) {
        var migrationSteps = new List<string>();
        // Logic to build downgrade migration steps based on differences
        // ...
        return migrationSteps;
    }
}
