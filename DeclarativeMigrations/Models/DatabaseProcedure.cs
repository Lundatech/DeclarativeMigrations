namespace Lundatech.DeclarativeMigrations.Models;

public class DatabaseProcedure {
    public DatabaseSchema ParentSchema { get; private set; }
    public string Name { get; private set; }
}
