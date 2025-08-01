using System;

using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

using NUnit.Framework;

namespace UnitTests;

public class DifferencesTests {
    [Test]
    public void GetMigrationToTargetSchema_ShouldReturnDifference_WhenTableHasBeenAddedInNewVersion() {
        var databaseSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(1, 0, 0));

        var targetSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(2, 0, 0));
        targetSchema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .Build();

        var options = new DatabaseServerOptions();
        var migration = databaseSchema.GetMigrationToTargetSchema(targetSchema, options);

        Assert.That(migration.IsEmpty(), Is.False);
        Assert.That(migration.Differences.Count, Is.EqualTo(1));

        var difference = migration.Differences[0];
        Assert.That(difference.Object, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.ObjectType.Table));
        Assert.That(difference.Type, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added));
        Assert.That(difference.Property.HasValue, Is.False);
        Assert.That(difference.DatabaseTable, Is.Null);
        Assert.That(difference.TargetTable!.Name, Is.EqualTo("new_table"));
    }


    [Test]
    public void GetMigrationToTargetSchema_ShouldReturnDifference_WhenTableColumnHasBeenAddedInNewVersion() {
        var databaseSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(1, 0, 0));
        databaseSchema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .Build();

        var targetSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(2, 0, 0));
        targetSchema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .WithColumn("name").AsString(100)
            .Build();

        var options = new DatabaseServerOptions();
        var migration = databaseSchema.GetMigrationToTargetSchema(targetSchema, options);

        Assert.That(migration.IsEmpty(), Is.False);
        Assert.That(migration.Differences.Count, Is.EqualTo(1));

        var difference = migration.Differences[0];
        Assert.That(difference.Object, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn));
        Assert.That(difference.Type, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added));
        Assert.That(difference.Property.HasValue, Is.False);
        Assert.That(difference.DatabaseTable, Is.Not.Null);
        Assert.That(difference.DatabaseTableColumn, Is.Null);
        Assert.That(difference.TargetTable, Is.Not.Null);
        Assert.That(difference.TargetTableColumn!.Type.Type, Is.EqualTo(DatabaseType.Standard.String));
        Assert.That(difference.TargetTableColumn!.Name, Is.EqualTo("name"));
    }

    [Test]
    public void GetMigrationToTargetSchema_ShouldReturnDifference_WhenTableColumnHasChangedTypeInNewVersion() {
        var databaseSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(1, 0, 0));
        databaseSchema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .Build();

        var targetSchema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(2, 0, 0));
        targetSchema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger64().AsPrimaryKey()
            .Build();

        var options = new DatabaseServerOptions();
        var migration = databaseSchema.GetMigrationToTargetSchema(targetSchema, options);

        Assert.That(migration.IsEmpty(), Is.False);
        Assert.That(migration.Differences.Count, Is.EqualTo(1));

        var difference = migration.Differences[0];
        Assert.That(difference.Object, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.ObjectType.TableColumn));
        Assert.That(difference.Type, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.DifferenceType.Altered));
        Assert.That(difference.Property, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.PropertyType.Type));
        Assert.That(difference.DatabaseTableColumn!.Type.Type, Is.EqualTo(DatabaseType.Standard.Integer32));
        Assert.That(difference.TargetTableColumn!.Type.Type, Is.EqualTo(DatabaseType.Standard.Integer64));
    }
}
