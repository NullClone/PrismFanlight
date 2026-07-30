using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightVariationFields
    {
        None = 0,
        StandingPositionSpread = 1 << 0,
        HeightVariation = 1 << 1,
        ArmExtensionVariation = 1 << 2,
        PenlightDirectionSpread = 1 << 3,
        ReactionDelaySeconds = 1 << 4,
        BeatJitterBeats = 1 << 5,
        EnergyResponse = 1 << 6,
        HandPositionSpread = 1 << 7,
        All = StandingPositionSpread | HeightVariation | ArmExtensionVariation | PenlightDirectionSpread | ReactionDelaySeconds | BeatJitterBeats | EnergyResponse | HandPositionSpread
    }
}
