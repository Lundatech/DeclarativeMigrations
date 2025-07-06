using System;

using Lundatech.DeclarativeMigrations.Models;

namespace UnitTests;

public class TableBuilderTests {
    [Test]
    public void AddStandardTable_ShouldAddTableToSchema() {
        var schema = new DatabaseSchema("schema_name", new Version(1, 0, 0));
        schema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .Build();

        Assert.That(schema.Tables.Count, Is.EqualTo(1), "Schema should contain one table.");
    }
}
