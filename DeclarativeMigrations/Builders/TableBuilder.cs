using System;
using System.Collections.Generic;
using System.Linq;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly DatabaseTable _table;
    private readonly TCustomTypeProvider _customTypeProvider;
    private readonly List<TableColumnBuilder<TCustomTypes, TCustomTypeProvider>> _columns = [];

    public TableBuilder(DatabaseSchema parentSchema, string tableName, TCustomTypeProvider customTypeProvider) {
        _table = new DatabaseTable(parentSchema, tableName);
        _customTypeProvider = customTypeProvider;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        return new TableColumnBuilder<TCustomTypes, TCustomTypeProvider>(this, _table, _customTypeProvider, columnName);
    }

    public DatabaseTable Build() {
        _table.SetColumns(_columns.Select(x => x.BuildColumn()));
        return _table;
    }
}
