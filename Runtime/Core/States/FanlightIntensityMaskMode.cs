using UnityEngine;

namespace PrismFanlight.Core
{
    internal enum FanlightIntensityMaskMode
    {
        None = 0,
        Pulse = 1,

        [InspectorName("Block Pulse")]
        BlockAlternatingPulse = 6,
        TravelingWave = 2,
        RadialWave = 3,
        AngularWave = 5,

        [InspectorName("Sparkle")]
        RandomSparkle = 4,
    }
}
