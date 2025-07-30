using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    public override async Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options) {
        if (!await SchemaExists(schema.Name)) {
            await ExecuteScript($"CREATE SCHEMA \"{schema.Name}\"", options);
        }
    }

    public override string GetQuotedTableColumnName(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return $"\"{tableColumn.Name}\"";
    }
    
    public override string GetTableColumnDataTypeScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return tableColumn.Type.Type switch {
            DatabaseType.Standard.Binary => "BYTEA",
            DatabaseType.Standard.Boolean => "BOOLEAN",
            DatabaseType.Standard.DateTime => "TIMESTAMP",
            DatabaseType.Standard.DateTimeOffset => "TIMESTAMP WITH TIME ZONE",
            // DatabaseType.Standard.Decimal => "DECIMAL",
            DatabaseType.Standard.Guid  => "UUID",
            DatabaseType.Standard.Integer32 => "INT",
            DatabaseType.Standard.Integer64 => "BIGINT",
            DatabaseType.Standard.SerialInteger32 => "INT",
            DatabaseType.Standard.SerialInteger64 => "BIGINT",
            DatabaseType.Standard.String => tableColumn.Type.Length.HasValue ? $"VARCHAR({tableColumn.Type.Length.Value})" : "TEXT",
            DatabaseType.Standard.TimeSpan => "INTERVAL",
            DatabaseType.Standard.DatabaseObjectId => "OID",
            _ => throw new NotImplementedException()
        };
    }
    
    public override List<string> GetTableColumnExtraCreateScripts(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        var extraScripts = new List<string>();

        if (tableColumn.IsNullable) {
            extraScripts.Add("NULL");
        }
        else {
            extraScripts.Add("NOT NULL");
        }
        
        return extraScripts;
    }

    public override List<string> GetTableExtraCreateScripts(DatabaseTable table, DatabaseServerOptions options) {
        var extraScripts = new List<string>();
        
        var uniqueColumns = table.Columns.Values.Where(x => x.IsUnique).ToList();
        if (uniqueColumns.Any())
            extraScripts.Add($"UNIQUE({string.Join(", ", uniqueColumns.Select(x => GetQuotedTableColumnName(x, options)))})");
        
        return extraScripts;
    }

    public override string GetQuotedTableName(DatabaseTable table, DatabaseServerOptions options) {
        return $"\"{table.ParentSchema.Name}\".\"{table.Name}\"";
    }
    
    public override async Task ExecuteScript(string script, DatabaseServerOptions options) {
        await EnsureConnectionIsOpen();
        await using var command = new NpgsqlCommand(script, _connection, _transaction);
        await command.ExecuteNonQueryAsync();
    }
}
