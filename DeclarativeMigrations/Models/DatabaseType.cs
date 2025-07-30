using System;

namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseType {
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
}
