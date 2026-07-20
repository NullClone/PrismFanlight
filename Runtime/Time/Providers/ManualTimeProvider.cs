using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal sealed class ManualTimeProvider : IShowTimeProvider
    {
        // Fields

        private double _seconds;
        private double _rate;


        // Methods

        public void Set(double seconds, double rate)
        {
            _seconds = seconds;
            _rate = rate;
        }

        public ShowTimeProviderSample Sample() => new(
            _seconds,
            _rate,
            _rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready,
            FanlightTimeDiscontinuity.None);
    }
}
