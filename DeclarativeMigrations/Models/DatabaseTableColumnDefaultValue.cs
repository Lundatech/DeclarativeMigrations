using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableColumnDefaultValue {
    public enum DefaultValueType {
        FixedBoolean,
        FixedGuid,
        CurrentDateTime,
        CurrentDateTimeUtc,
        RandomGuid,
    }

    public DefaultValueType Type { get; private set; }
    public bool? BooleanValue { get; private set; }
    public Guid? GuidValue { get; private set; }

    public DatabaseTableColumnDefaultValue(DefaultValueType type, bool? booleanValue = null, Guid? guidValue = null) {
        Type = type;
        BooleanValue = booleanValue;
        GuidValue = guidValue;
    }

    protected bool Equals(DatabaseTableColumnDefaultValue other) {
        return Type == other.Type && BooleanValue == other.BooleanValue && Nullable.Equals(GuidValue, other.GuidValue);
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DatabaseTableColumnDefaultValue)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine((int)Type, BooleanValue, GuidValue);
    }

    public static bool operator ==(DatabaseTableColumnDefaultValue? left, DatabaseTableColumnDefaultValue? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DatabaseTableColumnDefaultValue? left, DatabaseTableColumnDefaultValue? right) {
        return !Equals(left, right);
    }
}