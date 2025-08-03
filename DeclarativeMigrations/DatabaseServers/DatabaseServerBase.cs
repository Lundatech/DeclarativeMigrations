using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers;

internal abstract class DatabaseServerBase {
    public abstract Task EnsureConnectionIsOpen();

    public abstract Task<DatabaseSchema> ReadSchema(string schemaName, DatabaseServerOptions options);

    public async Task ApplySchemaMigration(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        // create schema if missing
        await CreateSchemaIfMissing(schemaMigration.DatabaseSchema, options);

        // create missing sequences
        await CreateMissingSequences(schemaMigration, options);

        // create missing tables
        await CreateMissingTables(schemaMigration, options);

        // create missing table columns
        await CreateMissingTableColumns(schemaMigration, options);

        // handle altered table columns
        await AlterTableColumns(schemaMigration, options);

        // optionally drop redundant table columns
        await DropRedundantTableColumns(schemaMigration, options);

        // optionally drop redundant tables
        await DropRedundantTables(schemaMigration, options);

        // optionally drop redundant sequences
        await DropRedundantSequences(schemaMigration, options);

        // drop redundant table indexes before creating to minimize resource usage on the database server
        await DropRedundantTableIndexes(schemaMigration, options);

        // create missing table indexes
        await CreateMissingTableIndexes(schemaMigration, options);

        // handle altered tables
        await AlterTables(schemaMigration, options);

        // handle altered table indexes
        await AlterTableIndexes(schemaMigration, options);

        // update schema version in database
        await UpdateSchemaVersion(schemaMigration.TargetSchema, options);

        var unhandledDifferences = schemaMigration.Differences.Where(x => !x.IsHandled).ToList();
        if (unhandledDifferences.Count > 0) throw new InvalidOperationException("Not all schema differences were handled during migration.");
    }

    public abstract Task UpdateSchemaVersion(DatabaseSchema schemaMigration, DatabaseServerOptions options);

    public async Task CreateMissingSequences(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var sequencesToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Sequence && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in sequencesToCreate) {
            await CreateSequence(difference, options);
            difference.SetHandled();
        }
    }

    public async Task CreateMissingTables(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tablesToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in tablesToCreate) {
            await CreateTable(difference, options);
            difference.SetHandled();
        }
    }

    public async Task CreateMissingTableColumns(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tableColumnsToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in tableColumnsToCreate) {
            await CreateTableColumn(difference, options);
            difference.SetHandled();
        }
    }

    public async Task AlterTableColumns(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tableColumnsToAlter = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Altered).ToList();
        foreach (var difference in tableColumnsToAlter) {
            await AlterTableColumn(difference, options);
            difference.SetHandled();
        }
    }

    public async Task CreateMissingTableIndexes(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var indexesToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableIndex && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in indexesToCreate) {
            await CreateTableIndex(difference.TargetTableIndex!, options);
            difference.SetHandled();
        }
    }

    public async Task DropRedundantTableIndexes(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var indexesToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableIndex && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        foreach (var difference in indexesToDrop) {
            await DropTableIndex(difference.DatabaseTableIndex!, options);
            difference.SetHandled();
        }
    }

    public async Task AlterTableIndexes(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var indexesToAlter = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableIndex && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Altered).ToList();
        foreach (var difference in indexesToAlter) {
            // handle this by dropping and recreating the index
            await DropTableIndex(difference.DatabaseTableIndex!, options);
            await CreateTableIndex(difference.TargetTableIndex!, options);
            difference.SetHandled();
        }
    }

    public async Task AlterTables(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var indexesToAlter = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Altered).ToList();
        foreach (var difference in indexesToAlter) {
            await AlterTable(difference, options);
            difference.SetHandled();
        }
    }

    public virtual async Task AlterTable(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.PrimaryKey) {
            await AlterTablePrimaryKey(difference, options);
        }
        else if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.Unique) {
            await AlterTableUnique(difference, options);
        }
        else {
            throw new NotSupportedException($"Property '{difference.Property}' is not supported for altering tables.");
        }
    }

    public abstract Task AlterTablePrimaryKey(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    public abstract Task AlterTableUnique(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    
    public virtual async Task AlterTableColumn(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.Type) {
            await AlterTableColumnType(difference, options);
        }
        else if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.DefaultValue) {
            await AlterTableColumnDefaultValue(difference, options);
        }
        else if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.Nullability) {
            await AlterTableColumnNullability(difference, options);
        }
        else if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.Unique) {
            await AlterTableColumnUnique(difference, options);
        }
        else if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.PrimaryKey) {
            await AlterTableColumnPrimaryKey(difference, options);
        }
        else if (difference.Property == DatabaseSchemaMigration.SchemaDifference.PropertyType.ForeignReference) {
            await AlterTableColumnForeignReference(difference, options);
        }
        else {
            throw new NotSupportedException($"Property '{difference.Property}' is not supported for altering table columns.");
        }
        //var script = $"ALTER TABLE {GetQuotedTableName(difference.DatabaseTable!, options)} ALTER COLUMN {GetTableColumnCreateScript(difference.TargetTableColumn!, options)}";
        //await ExecuteScript(script, options);
    }

    public abstract Task AlterTableColumnType(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    public abstract Task AlterTableColumnDefaultValue(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    public abstract Task AlterTableColumnNullability(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    public abstract Task AlterTableColumnUnique(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    public abstract Task AlterTableColumnPrimaryKey(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);
    public abstract Task AlterTableColumnForeignReference(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options);

    public async Task DropRedundantTableColumns(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tableColumnsToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop table columns when downgrading
        foreach (var difference in tableColumnsToDrop) {
            if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedTableColumnsOnUpgrade) {
                await DropTableColumn(difference, options);
            }
            difference.SetHandled();
        }
    }

    public async Task DropRedundantTables(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tablesToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop tables when downgrading
        foreach (var difference in tablesToDrop) {
            if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedTablesOnUpgrade) {
                await DropTable(difference, options);
            }
            difference.SetHandled();
        }
    }

    public async Task DropRedundantSequences(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var sequencesToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Sequence && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop sequences when downgrading
        foreach (var difference in sequencesToDrop) {
            if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedSequencesOnUpgrade) {
                await DropSequence(difference, options);
            }
            difference.SetHandled();
        }
    }

    public abstract Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options);

    public virtual async Task CreateSequence(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"CREATE SEQUENCE {GetQuotedSequenceName(difference.TargetSequence!, options)}";
        await ExecuteScript(script, options);
    }

    public abstract string GetQuotedSequenceName(DatabaseSequence sequence, DatabaseServerOptions options);

    public virtual async Task DropSequence(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"DROP SEQUENCE {GetQuotedSequenceName(difference.DatabaseSequence!, options)}";
        await ExecuteScript(script, options);
    }

    public virtual async Task CreateTable(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"CREATE TABLE {GetQuotedTableName(difference.TargetTable!, options)} ({string.Join(", ", difference.TargetTable!.Columns.Values.Select(x => GetTableColumnCreateScript(x, options)))}{GetTableExtraCreateScript(difference.TargetTable!, options)})";
        await ExecuteScript(script, options);
    }

    public virtual async Task CreateTableIndex(DatabaseTableIndex tableIndex, DatabaseServerOptions options) {
        var script = $"CREATE INDEX {GetQuotedTableIndexName(tableIndex, false, options)} ON {GetQuotedTableName(tableIndex.ParentTable, options)} ({string.Join(", ", tableIndex.ColumnNames.Select(x => GetQuotedTableColumnName(tableIndex.ParentTable.Columns[x], options)))})";
        await ExecuteScript(script, options);
    }

    public abstract string GetQuotedTableIndexName(DatabaseTableIndex tableIndex, bool includeSchema, DatabaseServerOptions options);

    public virtual async Task CreateTableColumn(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"ALTER TABLE {GetQuotedTableName(difference.DatabaseTable!, options)} ADD COLUMN {GetTableColumnCreateScript(difference.TargetTableColumn!, options)}";
        await ExecuteScript(script, options);
    }

    public virtual string GetTableColumnCreateScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return $"{GetQuotedTableColumnName(tableColumn, options)} {GetTableColumnDataTypeScript(tableColumn, options)}{GetTableColumnExtraCreateScript(tableColumn, options)}";
    }

    public abstract string GetQuotedTableColumnName(DatabaseTableColumn tableColumn, DatabaseServerOptions options);

    public abstract string GetTableColumnDataTypeScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options);

    public virtual string GetTableColumnExtraCreateScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        var extraScripts = GetTableColumnExtraCreateScripts(tableColumn, options);
        if (!extraScripts.Any()) return string.Empty;
        return $" {string.Join(" ", extraScripts)}";
    }

    public abstract List<string> GetTableColumnExtraCreateScripts(DatabaseTableColumn tableColumn, DatabaseServerOptions options);

    public virtual string GetTableExtraCreateScript(DatabaseTable table, DatabaseServerOptions options) {
        var extraScripts = GetTableExtraCreateScripts(table, options);
        if (!extraScripts.Any()) return string.Empty;
        return $", {string.Join(", ", extraScripts)}";
    }

    public abstract List<string> GetTableExtraCreateScripts(DatabaseTable table, DatabaseServerOptions options);

    public virtual async Task DropTable(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"DROP TABLE {GetQuotedTableName(difference.DatabaseTable!, options)}";
        await ExecuteScript(script, options);
    }

    public virtual async Task DropTableIndex(DatabaseTableIndex tableIndex, DatabaseServerOptions options) {
        var script = $"DROP INDEX {GetQuotedTableIndexName(tableIndex, true, options)}";
        await ExecuteScript(script, options);
    }

    public virtual async Task DropTableColumn(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"ALTER TABLE {GetQuotedTableName(difference.DatabaseTable!, options)} DROP COLUMN {GetQuotedTableColumnName(difference.DatabaseTableColumn!, options)}";
        await ExecuteScript(script, options);
    }

    public abstract string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options);
    public abstract string GetSequenceName(DatabaseTableColumn tableColumn);
    public abstract string GetIndexName(DatabaseTable table, List<string> columnNames);
    public abstract Task ExecuteScript(string script, DatabaseServerOptions options);

    public virtual void TableBuilderHook(DatabaseTable table) {
    }

    public virtual void TableColumnBuilderHook(DatabaseTableColumn tableColumn) {
    }

    public virtual void TableIndexBuilderHook(DatabaseTableIndex tableIndex) {
    }
}
