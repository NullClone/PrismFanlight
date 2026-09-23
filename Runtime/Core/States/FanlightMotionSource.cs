using PrismFanlight.Authoring;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightMotionSource
    {
        // Properties

        internal FanlightMotionAsset Asset { get; }
        internal float BeatsPerCycle { get; }
        internal float PhaseOffsetBeats { get; }

        // Methods

        internal FanlightMotionSource(FanlightMotionAsset asset, float beatsPerCycle, float phaseOffsetBeats)
        {
            Asset = asset;
            BeatsPerCycle = FanlightStateValidation.RequireRange(beatsPerCycle, 0.001f, 64f, nameof(beatsPerCycle));
            PhaseOffsetBeats = FanlightStateValidation.RequireRange(phaseOffsetBeats, -64f, 64f, nameof(phaseOffsetBeats));
        }
    }
}
