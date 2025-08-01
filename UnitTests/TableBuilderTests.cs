using System;

using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

using NUnit.Framework;

namespace UnitTests;

public class TableBuilderTests {
    [Test]
    public void AddStandardTable_ShouldAddTableToSchema() {
        var schema = new DatabaseSchema(DatabaseServerType.PostgreSql, "schema_name", new Version(1, 0, 0));
        
        schema.AddStandardTable("new_table")
            .WithColumn("id").AsInteger32().AsPrimaryKey()
            .Build();

        Assert.That(schema.Tables.Count, Is.EqualTo(1), "Schema should contain one table.");
        Assert.That(schema.Tables["new_table"].Name, Is.EqualTo("new_table"), "Table name should match.");
    }
}
