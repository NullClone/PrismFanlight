namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowTimeSample
    {
        // Properties

        internal double Seconds { get; }

        internal double Rate { get; }

        internal FanlightClockStatus Status { get; }

        internal FanlightTimeDiscontinuity Discontinuity { get; }

        internal bool IsFallbackActive { get; }

        internal bool IsPrimaryAvailable { get; }

        internal FanlightMusicalPosition MusicalPosition { get; }


        // Methods

        internal FanlightShowTimeSample(
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

        internal bool IsComplete =>
            IsFinite(Seconds)
            && IsFinite(Rate)
            && ((Status == FanlightClockStatus.Ready && Rate != 0d)
                || (Status == FanlightClockStatus.Holding && Rate == 0d))
            && MusicalPosition.IsComplete;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
