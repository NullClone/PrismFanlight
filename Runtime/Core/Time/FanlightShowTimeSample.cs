using System;

namespace PrismFanlight.Core
{
    public readonly struct FanlightShowTimeSample
    {
        // Properties

        public double Seconds { get; }

        public double Rate { get; }

        public FanlightClockStatus Status { get; }

        public FanlightTimeDiscontinuity Discontinuity { get; }

        public bool IsFallbackActive { get; }

        public bool IsPrimaryAvailable { get; }

        public FanlightMusicalPosition MusicalPosition { get; }


        // Methods

        public FanlightShowTimeSample(
            double seconds,
            double rate,
            FanlightClockStatus status,
            FanlightTimeDiscontinuity discontinuity,
            bool isFallbackActive,
            bool isPrimaryAvailable,
            FanlightMusicalPosition musicalPosition)
        {
            Seconds = seconds;
            Rate = rate;
            Status = status;
            Discontinuity = discontinuity;
            IsFallbackActive = isFallbackActive;
            IsPrimaryAvailable = isPrimaryAvailable;
            MusicalPosition = musicalPosition;
        }

        public bool IsComplete =>
            IsFinite(Seconds)
            && IsFinite(Rate)
            && ((Status == FanlightClockStatus.Ready && Rate != 0d)
                || (Status == FanlightClockStatus.Holding && Rate == 0d))
            && MusicalPosition.IsComplete
            && Math.Abs(Seconds - MusicalPosition.Seconds) <= 1e-9;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
