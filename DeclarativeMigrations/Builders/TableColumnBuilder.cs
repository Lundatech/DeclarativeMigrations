using System;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableColumnBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly TableBuilder<TCustomTypes, TCustomTypeProvider> _parentTableBuilder;
    private readonly DatabaseTable _parentTable;
    private readonly TCustomTypeProvider _customTypeProvider;
    private readonly string _columnName;

    private DatabaseType? _type = null;
    private bool _isPrimaryKey = false;
    private bool _isNullable = false;
    private DatabaseTableColumnDefaultValue? _defaultValue = null;
    private DatabaseTableColumnForeignReference? _foreignReference = null;

    public TableColumnBuilder(TableBuilder<TCustomTypes, TCustomTypeProvider> parentTableBuilder, DatabaseTable parentTable, TCustomTypeProvider customTypeProvider, string columnName) {
        _parentTableBuilder = parentTableBuilder;
        _parentTable = parentTable;
        _customTypeProvider = customTypeProvider;
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

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsInteger32() {
        SetType(new DatabaseType(DatabaseType.Standard.Integer32));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsInteger64() {
        SetType(new DatabaseType(DatabaseType.Standard.Integer64));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsDecimal(int precision, int scale) {
        SetType(new DatabaseType(DatabaseType.Standard.Decimal, null, precision, scale));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsBoolean() {
        SetType(new DatabaseType(DatabaseType.Standard.Boolean));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsGuid() {
        SetType(new DatabaseType(DatabaseType.Standard.Guid));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsDateTime() {
        SetType(new DatabaseType(DatabaseType.Standard.DateTime));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsZonedDateTime() {
        SetType(new DatabaseType(DatabaseType.Standard.ZonedDateTime));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsTimeSpan() {
        SetType(new DatabaseType(DatabaseType.Standard.TimeSpan));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsBinary(int length) {
        SetType(new DatabaseType(DatabaseType.Standard.Binary, length));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsCustomType(TCustomTypes customType) {
        SetType(_customTypeProvider.TranslateCustomType(customType));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsPrimaryKey() {
        _isPrimaryKey = true;
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        return _parentTableBuilder.WithColumn(columnName);
    }

    public DatabaseTableColumn BuildColumn() {
        return new DatabaseTableColumn(_parentTable, _columnName, _type, _isNullable, _isPrimaryKey, _defaultValue, _foreignReference);
    }

    public DatabaseTable Build() {
        return _parentTableBuilder.Build();
    }
}
