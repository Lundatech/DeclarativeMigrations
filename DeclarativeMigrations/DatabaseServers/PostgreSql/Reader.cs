using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lundatech.DeclarativeMigrations.DatabaseServers;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.DatabaseServers.PostgreSql;

internal partial class PostgreSqlDatabaseServer {
    internal async Task<DatabaseSchema> ReadSchemaFromServer(string schemaName, DatabaseServerOptions options) {
        // read schema version from server
        var version = new Version(0, 0, 0);

        var schema = new DatabaseSchema(schemaName, version);

        return schema;
    }
}