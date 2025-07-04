using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTable {
    private ImmutableList<DatabaseTableColumn>? _columns = null;

    public DatabaseSchema ParentSchema { get; private set; }
    public string Name { get; private set; }
    public ImmutableList<DatabaseTableColumn> Columns => _columns ?? throw new Exception("Columns have not been set. Use SetColumns method to initialize them.");

    public DatabaseTable(DatabaseSchema parentSchema, string name) {
        ParentSchema = parentSchema;
        Name = name;
    }

    public void SetColumns(IEnumerable<DatabaseTableColumn> columns) {
        if (columns.Any(x => x.ParentTable != this))
            throw new Exception("All columns must belong to the same table.");

        _columns = columns.ToImmutableList();
    }
}
