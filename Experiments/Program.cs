using System;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

namespace Experiments;

internal enum ApplicationDatabaseTypes {
    CustomerId,
    UserId,
}

internal class ApplicationDatabaseTypeProvider : ICustomTypeProvider<ApplicationDatabaseTypes> {
    public DatabaseType TranslateCustomType(ApplicationDatabaseTypes customType) {
        return customType switch {
            ApplicationDatabaseTypes.CustomerId => new DatabaseType(DatabaseType.Standard.Guid),
            ApplicationDatabaseTypes.UserId => new DatabaseType(DatabaseType.Standard.String, 200),
            _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
        };
    }
}

internal class Program {
    static async Task Main(string[] args) {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder {
            Host = "127.0.0.1",
            Port = 41002,
            Database = "tenant",
            Username = "postgres",
            Password = "password"
        };
        var connectionString = connectionStringBuilder.ToString();

        var databaseServer = new DatabaseServer(DatabaseServerType.PostgreSql, connectionString);

        // create target schema
        var targetSchema = new DatabaseSchema("haip", new Version(1, 0, 1));

        targetSchema.AddTable<ApplicationDatabaseTypes, ApplicationDatabaseTypeProvider>("application_database_types", new ApplicationDatabaseTypeProvider())
            .WithColumn("customer_id").AsCustomType(ApplicationDatabaseTypes.CustomerId)
            .WithColumn("name").AsString(100)
            .Build();

        targetSchema.AddStandardTable("standard_table")
            .WithColumn("id").AsString(100).AsPrimaryKey()
            .WithColumn("name").AsBoolean()
            .Build();

        await databaseServer.MigrateSchemaTo(targetSchema);
    }
}
