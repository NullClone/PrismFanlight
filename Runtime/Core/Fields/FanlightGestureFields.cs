using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightGestureFields
    {
        None = 0,
        BeatsPerCycle = 1 << 0,
        PhaseOffsetBeats = 1 << 1,
        HoldRatio = 1 << 2,
        Crispness = 1 << 3,
        FollowThrough = 1 << 4,
        DownbeatAccent = 1 << 5,
        All = BeatsPerCycle | PhaseOffsetBeats | HoldRatio | Crispness | FollowThrough | DownbeatAccent
    }
}
