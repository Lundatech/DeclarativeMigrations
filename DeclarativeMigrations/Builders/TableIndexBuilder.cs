using System;
using System.Collections.Generic;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableIndexBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly TableBuilder<TCustomTypes, TCustomTypeProvider> _parentTableBuilder;
    private readonly DatabaseTable _parentTable;
    private readonly DatabaseServerBase _databaseServer;
    private readonly List<string> _columnNames;
    
    internal TableIndexBuilder(TableBuilder<TCustomTypes, TCustomTypeProvider> parentTableBuilder, DatabaseTable parentTable, DatabaseServerBase databaseServer, List<string> columnNames) {
        _parentTableBuilder = parentTableBuilder;
        _parentTable = parentTable;
        _databaseServer = databaseServer;
        _columnNames = columnNames;
    }

    public DatabaseTableIndex BuildIndex() {
        var indexName = _databaseServer.GetIndexName(_parentTable, _columnNames);
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