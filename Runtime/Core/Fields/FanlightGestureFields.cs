using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightGestureFields
    {
        None = 0,
        BeatsPerCycle = 1 << 0,
        PhaseOffsetBeats = 1 << 1,
        StrokeRatio = 1 << 2,
        HoldRatio = 1 << 3,
        Crispness = 1 << 4,
        FollowThrough = 1 << 5,
        WristLagRatio = 1 << 6,
        DownbeatAccent = 1 << 7,
        All = BeatsPerCycle | PhaseOffsetBeats | StrokeRatio | HoldRatio | Crispness | FollowThrough | WristLagRatio | DownbeatAccent
    }
}
