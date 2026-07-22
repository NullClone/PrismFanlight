using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightPoseFields
    {
        None = 0,
        ReadyHandOffset = 1 << 0,
        AccentHandOffset = 1 << 1,
        HandArcOffset = 1 << 2,
        ReadyPenlightDirection = 1 << 3,
        AccentPenlightDirection = 1 << 4,
        BodyLean = 1 << 5,
        All = ReadyHandOffset | AccentHandOffset | HandArcOffset | ReadyPenlightDirection | AccentPenlightDirection | BodyLean
    }
}
