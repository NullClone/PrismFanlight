using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal sealed class ManualTimeProvider : IShowTimeProvider
    {
        // Fields

        private double _seconds;
        private double _rate;
        private long _sequence;


        // Properties

        public string ProviderId { get; }


        // Methods

        public ManualTimeProvider(string providerId) => ProviderId = providerId;

        public void Set(double seconds, double rate)
        {
            _seconds = seconds;
            _rate = rate;
        }

        public ShowTimeProviderSample Sample() => new(
            ProviderId,
            _seconds,
            _rate,
            _rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready,
            FanlightTimeDiscontinuity.None,
            ++_sequence);
    }
}
