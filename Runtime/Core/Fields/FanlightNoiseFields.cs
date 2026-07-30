using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightNoiseFields
    {
        None = 0,
        PhaseAmount = 1 << 0,
        PositionAmount = 1 << 1,
        DirectionAmount = 1 << 2,
        All = PhaseAmount | PositionAmount | DirectionAmount
    }
}
