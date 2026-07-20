using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal sealed class UnityTimeProvider : IShowTimeProvider
    {
        // Fields

        private readonly UnityUnscaledTimeSource _time;


        // Methods

        internal UnityTimeProvider(UnityUnscaledTimeSource time)
        {
            _time = time;
        }

        public ShowTimeProviderSample Sample() => new(
            _time.Seconds,
            1d,
            FanlightClockStatus.Ready,
            FanlightTimeDiscontinuity.None);
    }
}
