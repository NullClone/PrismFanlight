using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Prism Fanlight/Time Providers/Unity Time Provider")]
    public sealed class UnityTimeProvider : MonoBehaviour, IShowTimeProvider
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
