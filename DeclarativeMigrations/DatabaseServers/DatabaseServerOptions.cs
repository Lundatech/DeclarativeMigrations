namespace Lundatech.DeclarativeMigrations.DatabaseServers;

public class DatabaseServerOptions {
    //public string? TemporaryStorageSchemaName { get; set; } = null;
    public string MigrationTablesPrefix { get; set; } = "ltdm";
    public bool DropRemovedTablesOnUpgrade { get; set; } = false;
    public bool DropRemovedSequencesOnUpgrade { get; set; } = false;
}
