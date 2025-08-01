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
        
        // update schema version in database
        await UpdateSchemaVersion(schemaMigration.TargetSchema, options);;
    }

    public abstract Task UpdateSchemaVersion(DatabaseSchema schemaMigration, DatabaseServerOptions options);
    
    public virtual async Task CreateMissingSequences(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var sequencesToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Sequence && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in sequencesToCreate) {
            await CreateSequence(difference, options);
        }
    }
    
    public virtual async Task CreateMissingTables(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tablesToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in tablesToCreate) {
            await CreateTable(difference, options);
        }
    }
    
    public virtual async Task CreateMissingTableColumns(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tableColumnsToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in tableColumnsToCreate) {
            await CreateTableColumn(difference, options);
        }
    }
    
    public virtual async Task AlterTableColumns(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tableColumnsToAlter = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Altered).ToList();
        foreach (var difference in tableColumnsToAlter) {
            await AlterTableColumn(difference, options);
        }
    }

    public virtual async Task AlterTableColumn(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"ALTER TABLE {GetQuotedTableName(difference.DatabaseTable!, options)} ALTER COLUMN {GetTableColumnCreateScript(difference.DatabaseTableColumn!, options)}";
        await ExecuteScript(script, options);
    }
    
    public virtual async Task DropRedundantTableColumns(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tableColumnsToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop table columns when downgrading
        if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedTableColumnsOnUpgrade) {
            foreach (var difference in tableColumnsToDrop) {
                await DropTableColumn(difference, options);
            }
        }
    }
    
    public virtual async Task DropRedundantTables(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var tablesToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop tables when downgrading
        if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedTablesOnUpgrade) {
            foreach (var difference in tablesToDrop) {
                await DropTable(difference, options);
            }
        }
    }
    
    public virtual async Task DropRedundantSequences(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options) {
        var sequencesToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Sequence && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop sequences when downgrading
        if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedSequencesOnUpgrade) {
            foreach (var difference in sequencesToDrop) {
                await DropSequence(difference, options);
            }
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

    public virtual async Task CreateTableColumn(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"ALTER TABLE {GetQuotedTableName(difference.DatabaseTable!, options)} ADD COLUMN {GetTableColumnCreateScript(difference.DatabaseTableColumn!, options)}";
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
    
    public virtual async Task DropTableColumn(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"ALTER TABLE {GetQuotedTableName(difference.DatabaseTable!, options)} DROP COLUMN {GetQuotedTableColumnName(difference.DatabaseTableColumn!, options)}";
        await ExecuteScript(script, options);
    }
    
    public abstract string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options);
    public abstract Task ExecuteScript(string script, DatabaseServerOptions options);

    public virtual void TableBuilderHook(DatabaseTable table) {
    }
    
    public virtual void TableColumnBuilderHook(DatabaseTableColumn tableColumn) {
    }
}
