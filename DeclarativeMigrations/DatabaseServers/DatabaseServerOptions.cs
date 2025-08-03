namespace Lundatech.DeclarativeMigrations.DatabaseServers;

public class DatabaseServerOptions {
    public enum SameVersionDifferencesHandlingType {
        Error,
        TreatAsUpgrade,
    }
    
    public string MigrationDatabasePrefix { get; set; } = "_ltdm";
    public SameVersionDifferencesHandlingType SameVersionDifferencesHandling { get; set; } = SameVersionDifferencesHandlingType.Error;
    public bool DropRemovedTablesOnUpgrade { get; set; } = false;
    public bool DropRemovedSequencesOnUpgrade { get; set; } = false;
    public bool DropRemovedTableColumnsOnUpgrade { get; set; } = false;
}
