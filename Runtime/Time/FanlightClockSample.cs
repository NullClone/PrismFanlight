using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal readonly struct FanlightClockSample
    {
        // Properties

        internal double Seconds { get; }

        internal double Rate { get; }

        internal FanlightClockStatus Status { get; }

        internal FanlightTimeDiscontinuity Discontinuity { get; }

        internal bool IsFallbackActive { get; }

        internal bool IsPrimaryAvailable { get; }


        // Methods

        internal FanlightClockSample(
            double seconds,
            double rate,
            FanlightClockStatus status,
            FanlightTimeDiscontinuity discontinuity,
            bool isFallbackActive,
            bool isPrimaryAvailable)
        {
            Seconds = seconds;
            Rate = rate;
            Status = status;
            Discontinuity = discontinuity;
            IsFallbackActive = isFallbackActive;
            IsPrimaryAvailable = isPrimaryAvailable;
        }
    }
}
