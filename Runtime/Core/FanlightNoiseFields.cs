using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightNoiseFields
    {
        None = 0,
        PhaseAmount = 1 << 0,
        PhaseSpeed = 1 << 1,
        AxisAmount = 1 << 2,
        AxisSpeed = 1 << 3,
        Octaves = 1 << 4,
        Persistence = 1 << 5,
        All = PhaseAmount | PhaseSpeed | AxisAmount | AxisSpeed | Octaves | Persistence
    }
}
