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
        All = MotionAsset | BeatsPerCycle | PhaseOffsetBeats | BlockDelayXBeats | BlockDelayYBeats
    }
}
