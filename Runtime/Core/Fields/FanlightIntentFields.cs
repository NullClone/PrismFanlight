using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightIntentFields
    {
        None = 0,
        Energy = 1 << 0,
        Participation = 1 << 1,
        Synchronization = 1 << 2,
        All = Energy | Participation | Synchronization
    }
}
