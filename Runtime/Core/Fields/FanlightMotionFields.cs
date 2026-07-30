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
        BlockDelayXBeats = 1 << 3,
        BlockDelayYBeats = 1 << 4,
        MotionAmount = 1 << 5,
        HeightBias = 1 << 6,
        SideScale = 1 << 7,
        ForwardScale = 1 << 8,
        WristDelayRatio = 1 << 9,
        Variation = 1 << 10,
        All = MotionAsset | BeatsPerCycle | PhaseOffsetBeats | BlockDelayXBeats | BlockDelayYBeats | MotionAmount | HeightBias | SideScale | ForwardScale | WristDelayRatio | Variation
    }
}
