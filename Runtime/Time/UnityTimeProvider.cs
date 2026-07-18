using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal sealed class UnityTimeProvider : IShowTimeProvider
    {
        // Fields

        private readonly IUnscaledTimeSource _time;
        private long _sequence;


        // Properties

        public string ProviderId { get; }


        // Methods

        public UnityTimeProvider(string providerId, IUnscaledTimeSource time)
        {
            ProviderId = providerId;
            _time = time;
        }

        public ShowTimeProviderSample Sample() => new(
            ProviderId,
            _time.Seconds,
            1d,
            FanlightClockStatus.Ready,
            FanlightTimeDiscontinuity.None,
            ++_sequence);
    }
}
