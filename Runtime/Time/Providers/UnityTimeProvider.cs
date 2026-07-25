using System;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    [Serializable]
    public sealed class UnityTimeProvider : IShowTimeProvider
    {
        ShowTimeProviderSample IShowTimeProvider.Sample()
        {
            return new ShowTimeProviderSample(
                UnityEngine.Time.unscaledTimeAsDouble,
                1d,
                FanlightClockStatus.Ready,
                FanlightTimeDiscontinuity.None);
        }
    }
}
