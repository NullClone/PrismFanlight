using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightMotionFields
    {
        None = 0,
        MotionAsset = 1 << 0,
        BeatsPerCycle = 1 << 1,
        PhaseOffsetBeats = 1 << 2,
        MotionAmount = 1 << 3,
        HeightBias = 1 << 4,
        SideScale = 1 << 5,
        ForwardScale = 1 << 6,
        WristDelayRatio = 1 << 7,
        Variation = 1 << 8,
        All = MotionAsset | BeatsPerCycle | PhaseOffsetBeats | MotionAmount | HeightBias | SideScale | ForwardScale | WristDelayRatio | Variation
    }
}
