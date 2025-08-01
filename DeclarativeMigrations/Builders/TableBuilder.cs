using System;
using System.Collections.Generic;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class TableBuilder<TCustomTypes, TCustomTypeProvider> where TCustomTypes : Enum where TCustomTypeProvider : ICustomTypeProvider<TCustomTypes> {
    private readonly DatabaseSchema _parentSchema;
    private readonly DatabaseTable _table;
    private readonly TCustomTypeProvider _customTypeProvider;
    private readonly DatabaseServerBase _databaseServer;
    private readonly List<TableIndexBuilder<TCustomTypes, TCustomTypeProvider>> _indexBuilders = [];
    private readonly List<TableColumnBuilder<TCustomTypes, TCustomTypeProvider>> _columnBuilders = [];

    internal TableBuilder(DatabaseSchema parentSchema, string tableName, TCustomTypeProvider customTypeProvider, DatabaseServerBase databaseServer) {
        _parentSchema = parentSchema;
        _table = new DatabaseTable(parentSchema, tableName);
        _customTypeProvider = customTypeProvider;
        _databaseServer = databaseServer;
    }

    public TableIndexBuilder<TCustomTypes, TCustomTypeProvider> WithIndex(params string[] columnNames) {
        var indexBuilder = new TableIndexBuilder<TCustomTypes, TCustomTypeProvider>(this, _table, _customTypeProvider, _databaseServer, columnNames);
        _indexBuilders.Add(indexBuilder);
        return indexBuilder;
    }
    
    public TableColumnBuilder<TCustomTypes, TCustomTypeProvider> WithColumn(string columnName) {
        var columnBuilder = new TableColumnBuilder<TCustomTypes, TCustomTypeProvider>(this, _table, _customTypeProvider, _databaseServer, columnName);
        _columnBuilders.Add(columnBuilder);
        return columnBuilder;
    }

    public DatabaseTable Build() {
        foreach (var columnBuilder in _columnBuilders) {
            var column = columnBuilder.BuildColumn();
            _table.AddColumn(column);
            _databaseServer.TableColumnBuilderHook(column);
        }

        foreach (var indexBuilder in _indexBuilders) {
            var index = indexBuilder.BuildIndex();
            _table.AddIndex(index);
            _databaseServer.TableIndexBuilderHook(index);
        }
        
        _parentSchema.AddTable(_table);
        _databaseServer.TableBuilderHook(_table);
        return _table;
    }
}
