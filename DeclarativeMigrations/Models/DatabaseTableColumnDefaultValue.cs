namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseTableColumnDefaultValue {
    public enum DefaultValueType {
        FixedString,
        FixedInteger,
        CurrentDateTime,
        NextIntegerSequence,
        RandomGuid,
    }
    
    public DefaultValueType Type { get; private set; }
    
    
}
