using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightVariationFields
    {
        None = 0,
        SeatPosition = 1 << 0,
        BodyHeight = 1 << 1,
        ArmLength = 1 << 2,
        Angle = 1 << 3,
        DirectionSpread = 1 << 4,
        ReactionDelaySeconds = 1 << 5,
        BeatJitter = 1 << 6,
        BlockDelayXBeats = 1 << 7,
        BlockDelayYBeats = 1 << 8,
        EnergyResponse = 1 << 9,
        Speed = 1 << 10,
        BeatReactionDelaySeconds = 1 << 11,
        HandZone = 1 << 12,
        All = SeatPosition | BodyHeight | ArmLength | Angle | DirectionSpread | ReactionDelaySeconds | BeatJitter | BlockDelayXBeats | BlockDelayYBeats | EnergyResponse | Speed | BeatReactionDelaySeconds | HandZone
    }
}
