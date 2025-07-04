using System;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableColumnBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly TableBuilder<TCustomTypes, TCustomTypeProvider> _parentTableBuilder;
    private readonly DatabaseTable _parentTable;
    private readonly string _columnName;

    private DatabaseType? _type = null;
    private bool _isNullable = false;
    private DatabaseTableColumnDefaultValue? _defaultValue = null;

    public TableColumnBuilder(TableBuilder<TCustomTypes, TCustomTypeProvider> parentTableBuilder, DatabaseTable parentTable, string columnName) {
        _parentTableBuilder = parentTableBuilder;
        _parentTable = parentTable;
        _columnName = columnName;
    }

    private void SetType(DatabaseType type) {
        if (_type != null)
            throw new InvalidOperationException("Column type has already been specified.");
        _type = type;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsString(int length) {
        SetType(new DatabaseType(DatabaseType.Standard.String, length));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsCustomType(TCustomTypes customType) {
        SetType(new DatabaseType(Int));
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        return _parentTableBuilder.WithColumn(columnName);
    }

    public DatabaseTableColumn BuildColumn() {
        if (_type == null)
            throw new InvalidOperationException("Column type must be specified before building the column.");

        return new DatabaseTableColumn(_parentTable, _columnName, _type, _isNullable, _defaultValue);
    }

    public DatabaseTable Build() {
        return _parentTableBuilder.Build();
    }
}
