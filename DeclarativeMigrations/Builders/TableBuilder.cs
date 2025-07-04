using System;
using System.Collections.Generic;
using System.Linq;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly DatabaseTable _table;
    private readonly List<TableColumnBuilder<TCustomTypes, TCustomTypeProvider>> _columns = [];

    public TableBuilder(DatabaseSchema parentSchema, string tableName) {
        _table = new DatabaseTable(parentSchema, tableName);
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        return new TableColumnBuilder<TCustomTypes, TCustomTypeProvider>(this, _table, columnName);
    }

    public DatabaseTable Build() {
        _table.SetColumns(_columns.Select(x => x.BuildColumn()));
        return _table;
    }
}
