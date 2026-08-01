using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal enum ShowNegativeTimePolicy
    {
        ClampToZero = 0,
        AllowPreroll = 1
    }

    public readonly struct ShowTimeProviderSample
    {
        // Properties

        public double Seconds { get; }

        public double Rate { get; }

        public FanlightClockStatus Status { get; }

        public FanlightTimeDiscontinuity Discontinuity { get; }

        internal bool IsValid =>
            !double.IsNaN(Seconds)
            && !double.IsInfinity(Seconds)
            && !double.IsNaN(Rate)
            && !double.IsInfinity(Rate)
            && ((Status == FanlightClockStatus.Ready && Rate != 0d)
                || (Status == FanlightClockStatus.Holding && Rate == 0d))
            && Discontinuity is FanlightTimeDiscontinuity.None
                or FanlightTimeDiscontinuity.Seek
                or FanlightTimeDiscontinuity.Loop
                or FanlightTimeDiscontinuity.Reverse
                or FanlightTimeDiscontinuity.AuthorityChanged
                or FanlightTimeDiscontinuity.Reconnected;


        // Methods

        public ShowTimeProviderSample(
            double seconds,
            double rate,
            FanlightClockStatus status,
            FanlightTimeDiscontinuity discontinuity)
        {
            Seconds = seconds;
            Rate = rate;
            Status = status;
            Discontinuity = discontinuity;
        }
    }
}
