using System;

using Lundatech.DeclarativeMigrations.CustomTypes;
using Lundatech.DeclarativeMigrations.Models;

namespace Lundatech.DeclarativeMigrations.Builders;

public class NullCustomTypeProvider : ICustomTypeProvider<NullCustomTypes> {
    public DatabaseType TranslateCustomType(NullCustomTypes customType) {
        throw new NotSupportedException("NullCustomTypeProvider does not support custom types.");
    }
}
