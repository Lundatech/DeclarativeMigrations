using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseType : IEquatable<DatabaseType> {
    public enum Standard {
        String,
        Integer32,
        SerialInteger32,
        Integer64,
        SerialInteger64,
        Decimal,
        Boolean,
        Guid,
        DateTime,
        DateTimeOffset,
        TimeSpan,
        Binary,
        DatabaseObjectId
    }

    public Standard Type { get; private set; }
    public int? Length { get; private set; }
    public int? Precision { get; private set; }
    public int? Scale { get; private set; }

    public DatabaseType(Standard type, int? length = null, int? precision = null, int? scale = null) {
        Type = type;
        Length = length;
        Precision = precision;
        Scale = scale;
        if (length.HasValue && length <= 0)
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        if (precision.HasValue && precision <= 0)
            throw new ArgumentException("Precision must be greater than zero.", nameof(precision));
        if (scale.HasValue && scale < 0)
            throw new ArgumentException("Scale must be zero or greater.", nameof(scale));
    }
    
    public bool Equals(DatabaseType? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Length == other.Length && Precision == other.Precision && Scale == other.Scale;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DatabaseType)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine((int)Type, Length, Precision, Scale);
    }

    public static bool operator ==(DatabaseType? left, DatabaseType? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DatabaseType? left, DatabaseType? right) {
        return !Equals(left, right);
    }
    
    public override string ToString() {
        return $"{Type}{(Length.HasValue ? $" L={Length.Value}" : string.Empty)}{(Precision.HasValue ? $" P={Precision.Value}" : string.Empty)}{(Scale.HasValue ? $" S={Scale.Value}" : string.Empty)}";
    }
}
