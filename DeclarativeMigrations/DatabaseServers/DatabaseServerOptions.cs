namespace Lundatech.DeclarativeMigrations.DatabaseServers;

public class DatabaseServerOptions {
    //public string? TemporaryStorageSchemaName { get; set; } = null;
    public string MigrationTablesPrefix { get; set; } = "ltdm";
}
