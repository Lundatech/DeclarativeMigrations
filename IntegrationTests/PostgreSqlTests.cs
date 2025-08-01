using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

using Testcontainers.PostgreSql;

namespace IntegrationTests;

public class Tests {
    public const string SchemaName = "test_schema";

    private PostgreSqlContainer? _container = null;
    private string? _connectionString = null;
    private DatabaseSchema? _requiredSchema = null;

    [OneTimeSetUp]
    public async Task Setup() {
        var containerName = $"ltdm-pg-{DateTime.Now.Ticks}";
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17.5")
            .WithPortBinding(5432, true)
            .WithEnvironment(new Dictionary<string, string> {
                { "POSTGRES_USER", "postgres" },
                { "POSTGRES_DB", "tenant" },
                { "POSTGRES_PASSWORD", "password" },
                { "POSTGRESQL_LOG_CONNECTIONS", "yes" },
                { "POSTGRESQL_PGAUDIT_LOG", "all" },
            })
            //            .WithCreateParameterModifier(x => x.User = "postgres")
            .WithName(containerName)
            .Build();

        // need to make sure the read-write container is started before the read-only one
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString) {
            Database = "tenant",
            Username = "postgres",
            Password = "password"
        };
        _connectionString = connectionStringBuilder.ToString();
    }

    [OneTimeTearDown]
    public async Task Teardown() {
        if (_container != null) {
            await _container.DisposeAsync();
            _container = null;
        }
    }

    private async Task<DatabaseSchema> MigrateAndCheck() {
        var databaseServer = await DatabaseServer.Create(DatabaseServerType.PostgreSql, _connectionString!);
        await databaseServer.MigrateSchemaTo(_requiredSchema!);

        return await ConsistencyAsserts();
    }

    private async Task<DatabaseSchema> ConsistencyAsserts() {
        var databaseServer = await DatabaseServer.Create(DatabaseServerType.PostgreSql, _connectionString!);
        var databaseSchema = await databaseServer.ReadSchema(SchemaName);

        Assert.That(databaseSchema, Is.Not.Null);
        Assert.That(databaseSchema.Name, Is.EqualTo(SchemaName));
        Assert.That(databaseSchema.SchemaOrApplicationVersion, Is.EqualTo(_requiredSchema!.SchemaOrApplicationVersion));

        var migration = databaseSchema.GetMigrationToTargetSchema(_requiredSchema!, new DatabaseServerOptions());
        Assert.That(migration.IsEmpty(), Is.True);

        return databaseSchema;
    }

    [Test]
    [Order(1)]
    public async Task _01_InitialCreateTable() {
        _requiredSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, SchemaName, new Version(1, 0, 0));
        _requiredSchema.AddStandardTable("standard_table")
            .WithColumn("id").AsSerialInteger32().AsPrimaryKey()
            .WithColumn("name").AsString(100)
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(2)]
    public async Task _02_AddNewTable() {
        _requiredSchema!.AddStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(100)
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(2)]
    public async Task _02_AddTableIndex() {
        _requiredSchema!.RebuildStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(100)
            .WithIndex("id")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }
}
