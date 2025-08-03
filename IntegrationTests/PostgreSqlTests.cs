using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

using Npgsql;

using NUnit.Framework;

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

    private async Task<DatabaseSchema> MigrateAndCheck(Action<DatabaseServerOptions>? configure = null) {
        var databaseServer = await DatabaseServer.Create(DatabaseServerType.PostgreSql, _connectionString!, configure);
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
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.AddStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(100)
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(3)]
    public async Task _03_AddTableIndex() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(100)
            .WithIndex("id")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(4)]
    public async Task _04_AddTableColumn() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(100)
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithIndex("id")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(5)]
    public async Task _05_ModifyTableIndex() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(100)
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(6)]
    public async Task _06_ModifyStringTableColumnType() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(200)
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(7)]
    public async Task _07_AddIntegerTableColumn() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(200)
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithColumn("age").AsInteger32()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(8)]
    public async Task _08_ModifyIntegerTableColumnType() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("name").AsString(200)
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(9)]
    public async Task _09_DropTableColumn() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck(configure => {
            configure.DropRemovedTableColumnsOnUpgrade = true;
        });

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(2));
    }

    [Test]
    [Order(10)]
    public async Task _10_DropTable() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema.DropTable("standard_table");

        var databaseSchema = await MigrateAndCheck(configure => {
            configure.DropRemovedTablesOnUpgrade = true;
        });

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(11)]
    public async Task _11_ModifyTableColumnNullability() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithColumn("age").AsInteger64().AsNullable()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(12)]
    public async Task _12_ModifyTableColumnNullabilityBack() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(false)
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(13)]
    public async Task _13_ModifyTableColumnDefaultValue() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean().DefaultingToValue(true)
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(14)]
    public async Task _14_ModifyTableColumnDropDefaultValue() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(15)]
    public async Task _15_AddTablePrimaryKey() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean().AsPrimaryKey()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(16)]
    public async Task _16_DropTablePrimaryKeys() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(17)]
    public async Task _17_AddTablePrimaryKeyWhenThereWasNone() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(18)]
    public async Task _18_AddTableUniqueConstraint() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().AsUnique().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(19)]
    public async Task _19_ModifyTableUniqueConstraint() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().AsUnique().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean().AsUnique()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(20)]
    public async Task _20_DropTableUniqueConstraint() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.ReplaceStandardTable("new_table")
            .WithColumn("id").AsGuid().AsPrimaryKey().DefaultingToRandomGuid()
            .WithColumn("is_manager").AsBoolean()
            .WithColumn("age").AsInteger64()
            .WithIndex("id", "is_manager")
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }
    
    [Test]
    [Order(21)]
    public async Task _21_AddNewTableWithForeignReference() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.AddStandardTable("new_table2")
            .WithColumn("id").AsSerialInteger32().AsPrimaryKey()
            .WithColumn("name").AsString(200)
            .WithColumn("external_id").AsGuid().HavingReferenceTo("new_table", "id", CascadeType.Cascade)
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }
    [Test]
    [Order(22)]
    public async Task _22_DropForeignReference() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.AddStandardTable("new_table2")
            .WithColumn("id").AsSerialInteger32().AsPrimaryKey()
            .WithColumn("name").AsString(200)
            .WithColumn("external_id").AsGuid()
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

    [Test]
    [Order(23)]
    public async Task _23_AddForeignReference() {
        _requiredSchema!.IncrementVersion();
        _requiredSchema!.AddStandardTable("new_table2")
            .WithColumn("id").AsSerialInteger32().AsPrimaryKey()
            .WithColumn("name").AsString(200)
            .WithColumn("external_id").AsGuid().HavingReferenceTo("new_table", "id", CascadeType.Cascade)
            .Build();

        var databaseSchema = await MigrateAndCheck();

        Assert.That(databaseSchema.Tables.Count, Is.EqualTo(1));
    }

}
