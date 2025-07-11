using System;

using Lundatech.DeclarativeMigrations.Models;

namespace UnitTests;

public class DifferencesTests {
    [Test]
    public void GetMigrationToTargetSchema_ShouldReturnDifference_WhenTableHasBeenAddedInNewVersion() {
        var databaseSchema = new DatabaseSchema("schema_name", new Version(1, 0, 0));

        var targetSchema = new DatabaseSchema("schema_name", new Version(2, 0, 0));
        targetSchema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .Build();

        var migration = databaseSchema.GetMigrationToTargetSchema(targetSchema, new());

        Assert.That(migration.IsEmpty(), Is.False, "Migration should not be empty.");
        Assert.That(migration.Differences.Count, Is.EqualTo(1), "There should be one difference.");
        var difference = migration.Differences[0];
        Assert.That(difference.Object, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.ObjectType.Table), "Object type should be Table.");
        Assert.That(difference.Type, Is.EqualTo(DatabaseSchemaMigration.SchemaDifference.DifferenceType.Added), "Difference type should be Added.");
        Assert.That(difference.Property.HasValue, Is.False, "Property should not have a value for added tables.");
        Assert.That(difference.Table!.Name, Is.EqualTo("new_table"), "Table name should match the added table name.");
    }
}
