using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightGestureFields
    {
        None = 0,
        GestureId = 1 << 0,
        BeatsPerCycle = 1 << 1,
        PhaseOffsetBeats = 1 << 2,
        AttackRatio = 1 << 3,
        HoldRatio = 1 << 4,
        ReturnRatio = 1 << 5,
        Crispness = 1 << 6,
        FollowThrough = 1 << 7,
        DownbeatAccent = 1 << 8,
        All = GestureId | BeatsPerCycle | PhaseOffsetBeats | AttackRatio | HoldRatio | ReturnRatio | Crispness | FollowThrough | DownbeatAccent
    }
}
