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
        
        // create missing tables
        var tablesToCreate = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added).ToList();
        foreach (var difference in tablesToCreate) {
            await CreateTable(difference, options);
        }
        
        // optionally drop redundant tables
        var tablesToDrop = schemaMigration.Differences
            .Where(x => x.Object == DatabaseSchemaMigration.SchemaDifference.ObjectType.Table && x.Type == DatabaseSchemaMigration.SchemaDifference.DifferenceType.Dropped).ToList();
        // never drop tables when downgrading
        if (schemaMigration.Type == DatabaseSchemaMigration.MigrationType.Upgrade && options.DropRemovedTablesOnUpgrade) {
            foreach (var difference in tablesToDrop) {
                await DropTable(difference, options);
            }
        }
    }

    public abstract Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options);
    
    public virtual async Task CreateTable(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        var script = $"CREATE TABLE {GetQuotedTableName(difference.TargetTable!, options)} ({string.Join(", ", difference.TargetTable!.Columns.Values.Select(x => GetTableColumnCreateScript(x, options)))}{GetTableExtraCreateScript(difference.TargetTable!, options)})";
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
    
    public abstract string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options);
    public abstract Task ExecuteScript(string script, DatabaseServerOptions options);
}
