using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    public enum ShowNegativeTimePolicy
    {
        ClampToZero = 0,
        AllowPreroll = 1
    }

    public readonly struct ShowTimeProviderSample
    {
        public string ProviderId { get; }
        public double Seconds { get; }
        public double Rate { get; }
        public FanlightClockStatus Status { get; }
        public FanlightTimeDiscontinuity Discontinuity { get; }
        public long Sequence { get; }

        public ShowTimeProviderSample(
            string providerId,
            double seconds,
            double rate,
            FanlightClockStatus status,
            FanlightTimeDiscontinuity discontinuity,
            long sequence)
        {
            ProviderId = providerId ?? string.Empty;
            Seconds = seconds;
            Rate = rate;
            Status = status;
            Discontinuity = discontinuity;
            Sequence = sequence;
        }

        public bool IsValid =>
            !string.IsNullOrEmpty(ProviderId)
            && !double.IsNaN(Seconds) && !double.IsInfinity(Seconds)
            && !double.IsNaN(Rate) && !double.IsInfinity(Rate)
            && ((Status == FanlightClockStatus.Ready && Rate != 0d)
                || (Status == FanlightClockStatus.Holding && Rate == 0d));
    }

    public interface IShowTimeProvider
    {
        string ProviderId { get; }
        ShowTimeProviderSample Sample();
    }

    public interface IShowTempoMapResolver
    {
        string TempoMapId { get; }
        int Version { get; }
        FanlightMusicalPosition Evaluate(double seconds);
    }

    public interface IShowTimeCoordinator
    {
        string TimeDomainId { get; }
        int TimeDomainVersion { get; }
        ShowNegativeTimePolicy NegativeTimePolicy { get; }
        bool IsFallbackActive { get; }
        bool IsPrimaryAvailable { get; }
        bool TrySample(long evaluationId, out FanlightShowTimeSample sample, out FanlightShowTimeFault fault);
        bool TryRequestPrimaryReacquire(out string failureCode);
    }

    public interface IUnscaledTimeSource
    {
        double Seconds { get; }
    }
}
