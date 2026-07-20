using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightDirectionFields
    {
        None = 0,
        Mode = 1 << 0,
        WorldYawDegrees = 1 << 1,
        AimStrength = 1 << 2,
        All = Mode | WorldYawDegrees | AimStrength
    }
}
