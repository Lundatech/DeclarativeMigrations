using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    public override async Task UpdateSchemaVersion(DatabaseSchema targetSchema, DatabaseServerOptions options) {
        var (_, fullVersionTableName) = GetMigrationTableName(options, targetSchema.Name, "version");

        await using var command = new NpgsqlCommand($"""
            CREATE TABLE IF NOT EXISTS {fullVersionTableName} (version VARCHAR(50));
            DELETE FROM {fullVersionTableName};
            INSERT INTO {fullVersionTableName} (version) VALUES (@version);
            """, _connection, _transaction);
        command.Parameters.AddWithValue("version", targetSchema.SchemaOrApplicationVersion.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public override async Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options) {
        if (!await SchemaExists(schema.Name)) {
            await ExecuteScript($"CREATE SCHEMA \"{schema.Name}\"", options);
        }
    }

    public override string GetTableColumnDataTypeScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        return tableColumn.Type.Type switch {
            DatabaseType.Standard.Binary => "BYTEA",
            DatabaseType.Standard.Boolean => "BOOLEAN",
            DatabaseType.Standard.DateTime => "TIMESTAMP",
            DatabaseType.Standard.DateTimeOffset => "TIMESTAMP WITH TIME ZONE",
            // DatabaseType.Standard.Decimal => "DECIMAL",
            DatabaseType.Standard.Guid => "UUID",
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

        if (tableColumn.IsPrimaryKey) {
            extraScripts.Add("PRIMARY KEY");
        }

        if (tableColumn.ForeignReference != null) {
            extraScripts.Add(
                $"REFERENCES \"{tableColumn.ParentTable.ParentSchema.Name}\".\"{tableColumn.ForeignReference.ForeignTableName}\" (\"{tableColumn.ForeignReference.ForeignColumnName}\") ON DELETE {GetCascadeTypeScript(tableColumn.ForeignReference.OnDeleteCascadeType)}");
        }

        if (tableColumn.DefaultValue != null) {
            extraScripts.Add($"DEFAULT {GetDefaultValueScript(tableColumn.DefaultValue)}");
        }

        if (tableColumn.Type.Type == DatabaseType.Standard.SerialInteger32 || tableColumn.Type.Type == DatabaseType.Standard.SerialInteger64) {
            extraScripts.Add($"DEFAULT (nextval('{GetSequenceName(tableColumn)}'::REGCLASS))");
        }

        return extraScripts;
    }

    private string GetDefaultValueScript(DatabaseTableColumnDefaultValue defaultValue) {
        switch (defaultValue.Type) {
            case DatabaseTableColumnDefaultValue.DefaultValueType.CurrentDateTime:
                return "CURRENT_TIMESTAMP";
            case DatabaseTableColumnDefaultValue.DefaultValueType.CurrentDateTimeUtc:
                return "(now() AT TIME ZONE 'utc'::TEXT)";
            case DatabaseTableColumnDefaultValue.DefaultValueType.FixedBoolean:
                return defaultValue.BooleanValue!.Value ? "true" : "false";
            case DatabaseTableColumnDefaultValue.DefaultValueType.FixedGuid:
                return $"'{defaultValue.GuidValue!.Value}'::UUID";
            case DatabaseTableColumnDefaultValue.DefaultValueType.RandomGuid:
                return "(gen_random_uuid())";
            default:
                throw new NotImplementedException($"Default value type {defaultValue.Type} is not implemented.");
        }
    }

    private string GetCascadeTypeScript(CascadeType? cascadeType) {
        return cascadeType switch {
            CascadeType.Cascade => "CASCADE",
            CascadeType.SetNull => "SET NULL",
            CascadeType.SetDefault => "SET DEFAULT",
            CascadeType.NoAction => "NO ACTION",
            CascadeType.Restrict => "RESTRICT",
            _ => throw new NotImplementedException()
        };
    }

    public override List<string> GetTableExtraCreateScripts(DatabaseTable table, DatabaseServerOptions options) {
        var extraScripts = new List<string>();

        var uniqueColumns = table.Columns.Values.Where(x => x.IsUnique).ToList();
        if (uniqueColumns.Any())
            extraScripts.Add($"UNIQUE ({string.Join(", ", uniqueColumns.Select(x => GetQuotedTableColumnName(x, options)))})");

        return extraScripts;
    }

    public override async Task ExecuteScript(string script, DatabaseServerOptions options) {
        await EnsureConnectionIsOpen();
        await using var command = new NpgsqlCommand(script, _connection, _transaction);
        await command.ExecuteNonQueryAsync();
    }
}