using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightVisibilityFields
    {
        None = 0,
        PenlightsEnabled = 1 << 0,
        AudienceBodiesEnabled = 1 << 1,
        All = PenlightsEnabled | AudienceBodiesEnabled
    }
}
