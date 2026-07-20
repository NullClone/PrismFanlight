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
        Realism = 1 << 3,
        Reach = 1 << 4,
        All = Energy | Participation | Synchronization | Realism | Reach
    }
}
