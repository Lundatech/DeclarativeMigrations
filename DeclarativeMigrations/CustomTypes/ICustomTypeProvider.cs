using System;

using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.CustomTypes;

public interface ICustomTypeProvider<TCustomTypes> where TCustomTypes : Enum {
    public DatabaseType TranslateCustomType(TCustomTypes customType);
}
