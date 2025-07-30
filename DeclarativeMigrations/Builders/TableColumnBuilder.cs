using System;
using System.Linq;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableColumnBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly TableBuilder<TCustomTypes, TCustomTypeProvider> _parentTableBuilder;
    private readonly DatabaseTable _parentTable;
    private readonly TCustomTypeProvider _customTypeProvider;
    private readonly string _columnName;

    private DatabaseType? _type = null;
    private bool _isNullable = false;
    private bool _isPrimaryKey = false;
    private bool _isUnique = false;
    private string? _referencesTableName = null;
    private string? _referencesColumnName = null;
    private CascadeType? _onDeleteCascadeType = null;
    private DatabaseTableColumnDefaultValue? _defaultValue = null;
    
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
        SetType(new DatabaseType(DatabaseType.Standard.String, length: length));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsText() {
        SetType(new DatabaseType(DatabaseType.Standard.String));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsInteger32() {
        SetType(new DatabaseType(DatabaseType.Standard.Integer32));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsSerialInteger32() {
        SetType(new DatabaseType(DatabaseType.Standard.SerialInteger32));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsInteger64() {
        SetType(new DatabaseType(DatabaseType.Standard.Integer64));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsSerialInteger64() {
        SetType(new DatabaseType(DatabaseType.Standard.SerialInteger64));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsDecimal(int precision, int scale) {
        SetType(new DatabaseType(DatabaseType.Standard.Decimal, precision: precision, scale: scale));
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

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsDateTimeOffset() {
        SetType(new DatabaseType(DatabaseType.Standard.DateTimeOffset));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsTimeSpan() {
        SetType(new DatabaseType(DatabaseType.Standard.TimeSpan));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsBinary() {
        SetType(new DatabaseType(DatabaseType.Standard.Binary));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsDatabaseObjectId() {
        SetType(new DatabaseType(DatabaseType.Standard.DatabaseObjectId));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsCustomType(TCustomTypes customType) {
        SetType(_customTypeProvider.TranslateCustomType(customType));
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsNullable() {
        _isNullable = true;
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsPrimaryKey() {
        _isPrimaryKey = true;
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> AsUnique() {
        _isUnique = true;
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> HavingReferenceTo(string foreignTableName, CascadeType onDeleteCascadeType) {
        if (_referencesTableName != null) throw new InvalidOperationException($"Can not set foreign reference for {_columnName} twice");
        
        _referencesTableName = foreignTableName;
        _referencesColumnName = _columnName;
        _onDeleteCascadeType = onDeleteCascadeType;
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> HavingReferenceTo(string foreignTableName, string foreignColumnName, CascadeType onDeleteCascadeType) {
        if (_referencesTableName != null) throw new InvalidOperationException($"Can not set foreign reference for {_columnName} twice");
        
        _referencesTableName = foreignTableName;
        _referencesColumnName = foreignColumnName;
        _onDeleteCascadeType = onDeleteCascadeType;
        return this;
    }

    private void CheckDefaultValue(params DatabaseType.Standard[] allowedTypes) {
        if (_type == null) throw new InvalidOperationException($"Can not set default value for column {_columnName} before setting its type");
        if (allowedTypes.All(x => x != _type.Type))
            throw new InvalidOperationException($"Can not set default value for column {_columnName} since it is of type {_type.Type}");
    }
    
    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> DefaultingToRandomGuid() {
        CheckDefaultValue(DatabaseType.Standard.Guid);
        
        _defaultValue = new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.RandomGuid);
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> DefaultingToUtcNow() {
        CheckDefaultValue(DatabaseType.Standard.DateTime, DatabaseType.Standard.DateTimeOffset);

        _defaultValue = new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.CurrentDateTimeUtc);
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> DefaultingToNow() {
        CheckDefaultValue(DatabaseType.Standard.DateTime, DatabaseType.Standard.DateTimeOffset);

        _defaultValue = new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.CurrentDateTime);
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> DefaultingToValue(Guid defaultValue) {
        CheckDefaultValue(DatabaseType.Standard.Guid);

        _defaultValue = new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.FixedGuid, guidValue: defaultValue);
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> DefaultingToValue(bool defaultValue) {
        CheckDefaultValue(DatabaseType.Standard.Boolean);
        
        _defaultValue = new DatabaseTableColumnDefaultValue(DatabaseTableColumnDefaultValue.DefaultValueType.FixedBoolean, booleanValue: defaultValue);
        return this;
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        return _parentTableBuilder.WithColumn(columnName);
    }

    public DatabaseTableColumn BuildColumn() {
        if (_type == null) throw new InvalidOperationException($"Can not build column for {_columnName} since it has no type");
        DatabaseTableColumnForeignReference? foreignReference = null;
        if (_referencesTableName != null)
            foreignReference = new DatabaseTableColumnForeignReference(_referencesTableName, _referencesColumnName!, _onDeleteCascadeType!.Value);
        var column = new DatabaseTableColumn(_parentTable, _columnName, _type, _isNullable, _isPrimaryKey, _isUnique, _defaultValue, foreignReference);
        return column;
    }

    public DatabaseTable Build() {
        return _parentTableBuilder.Build();
    }
}
