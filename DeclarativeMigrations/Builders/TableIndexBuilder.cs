using System;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableIndexBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly TableBuilder<TCustomTypes, TCustomTypeProvider> _parentTableBuilder;
    private readonly DatabaseTable _parentTable;
    private readonly TCustomTypeProvider _customTypeProvider;
    private readonly DatabaseServerBase _databaseServer;
    private readonly string[] _columnNames;
    
    internal TableIndexBuilder(TableBuilder<TCustomTypes, TCustomTypeProvider> parentTableBuilder, DatabaseTable parentTable, TCustomTypeProvider customTypeProvider, DatabaseServerBase databaseServer, string[] columnNames) {
        _parentTableBuilder = parentTableBuilder;
        _parentTable = parentTable;
        _customTypeProvider = customTypeProvider;
        _databaseServer = databaseServer;
        _columnNames = columnNames;
    }

    public DatabaseTableIndex BuildIndex() {
        var indexName = $"ltdmidx_{_parentTable.Name}__{string.Join("__", _columnNames)}";
        return new DatabaseTableIndex(_parentTable, indexName, _columnNames);
    }

    public TableIndexBuilder<TCustomTypes, TCustomTypeProvider> WithIndex(params string[] columnNames) {
        return _parentTableBuilder.WithIndex(columnNames);
    }

    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        return _parentTableBuilder.WithColumn(columnName);
    }

    public DatabaseTable Build() {
        return _parentTableBuilder.Build();
    }
}