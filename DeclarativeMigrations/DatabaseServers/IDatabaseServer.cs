using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers;

internal interface IDatabaseServer {
    //Task<List<DatabaseSchema>> ReadAllSchemas();
    Task<DatabaseSchema> ReadSchema(string schemaName, DatabaseServerOptions options);
    Task ApplySchemaMigration(DatabaseSchemaMigration schemaMigration, DatabaseServerOptions options);
}
