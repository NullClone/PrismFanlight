using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    public enum ShowNegativeTimePolicy
    {
        ClampToZero = 0,
        AllowPreroll = 1
    }

    internal readonly struct ShowTimeProviderSample
    {
        internal double Seconds { get; }
        internal double Rate { get; }
        internal FanlightClockStatus Status { get; }
        internal FanlightTimeDiscontinuity Discontinuity { get; }

        internal ShowTimeProviderSample(
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

        internal bool IsValid =>
            !double.IsNaN(Seconds) && !double.IsInfinity(Seconds)
                                   && !double.IsNaN(Rate) && !double.IsInfinity(Rate)
                                   && ((Status == FanlightClockStatus.Ready && Rate != 0d)
                                       || (Status == FanlightClockStatus.Holding && Rate == 0d));
    }

    internal interface IShowTimeProvider
    {
        ShowTimeProviderSample Sample();
    }

    internal interface IShowTempoMapResolver
    {
        FanlightMusicalPosition Evaluate(double seconds);
    }
}
