using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using Lundatech.DeclarativeMigrations.Models;
using System;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.SqlServer;

internal partial class SqlServerDatabaseServer {
    public override Task UpdateSchemaVersion(DatabaseSchema schemaMigration, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task CreateSchemaIfMissing(DatabaseSchema schema, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }
  
    public override string GetTableColumnDataTypeScript(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }
    
    public override List<string> GetTableColumnExtraCreateScripts(DatabaseTableColumn tableColumn, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }
    
    public override List<string> GetTableExtraCreateScripts(DatabaseTable table, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableColumnType(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableColumnDefaultValue(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableColumnNullability(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableColumnUnique(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableColumnPrimaryKey(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableColumnForeignReference(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTableUnique(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override Task AlterTablePrimaryKey(DatabaseSchemaMigration.SchemaDifference difference, DatabaseServerOptions options) {
        throw new NotImplementedException();
    }

    public override async Task ExecuteScript(string script, DatabaseServerOptions options) {
        await using var command = new SqlCommand(script, _connection, _transaction);
        await command.ExecuteNonQueryAsync();
    }
}