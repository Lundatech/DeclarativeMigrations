using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableColumnForeignReference {
    // public DatabaseTableColumn ParentColumn { get; private set; }
    // public DatabaseTable ReferencedTable { get; private set; }
    // public DatabaseTableColumn ReferencedColumn { get; private set; }
    //
    // public DatabaseTableColumnForeignReference(DatabaseTableColumn parentColumn, DatabaseTable referencedTable, DatabaseTableColumn referencedColumn) {
    //     ParentColumn = parentColumn;
    //     ReferencedTable = referencedTable;
    //     ReferencedColumn = referencedColumn;
    // }
    
    public string ForeignTableName { get; private set; }
    public string ForeignColumnName { get; private set; }
    public CascadeType OnDeleteCascadeType { get; private set; }

    public DatabaseTableColumnForeignReference(string foreignTableName, string foreignColumnName, CascadeType onDeleteCascadeType) {
        ForeignTableName = foreignTableName;
        ForeignColumnName = foreignColumnName;
        OnDeleteCascadeType = onDeleteCascadeType;
    }
    
    protected bool Equals(DatabaseTableColumnForeignReference other) {
        return ForeignTableName == other.ForeignTableName && ForeignColumnName == other.ForeignColumnName && OnDeleteCascadeType == other.OnDeleteCascadeType;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DatabaseTableColumnForeignReference)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(ForeignTableName, ForeignColumnName, (int)OnDeleteCascadeType);
    }

    public static bool operator ==(DatabaseTableColumnForeignReference? left, DatabaseTableColumnForeignReference? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DatabaseTableColumnForeignReference? left, DatabaseTableColumnForeignReference? right) {
        return !Equals(left, right);
    }
}
