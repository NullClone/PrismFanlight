using System;

namespace PrismFanlight.Core
{
    public readonly struct FanlightShowTimeSample
    {
        // Properties

        public string TimeDomainId { get; }

        public int TimeDomainVersion { get; }

        public string ProviderId { get; }

        public string TempoMapId { get; }

        public int TempoMapVersion { get; }

        public double Seconds { get; }

        public double Rate { get; }

        public FanlightClockStatus Status { get; }

        public FanlightTimeDiscontinuity Discontinuity { get; }

        public long Sequence { get; }

        public bool IsFallbackActive { get; }

        public bool IsPrimaryAvailable { get; }

        public FanlightMusicalPosition MusicalPosition { get; }


        // Methods

        public FanlightShowTimeSample(
            string timeDomainId,
            int timeDomainVersion,
            string providerId,
            string tempoMapId,
            int tempoMapVersion,
            double seconds,
            double rate,
            FanlightClockStatus status,
            FanlightTimeDiscontinuity discontinuity,
            long sequence,
            bool isFallbackActive,
            bool isPrimaryAvailable,
            FanlightMusicalPosition musicalPosition)
        {
            TimeDomainId = timeDomainId ?? string.Empty;
            TimeDomainVersion = timeDomainVersion;
            ProviderId = providerId ?? string.Empty;
            TempoMapId = tempoMapId ?? string.Empty;
            TempoMapVersion = tempoMapVersion;
            Seconds = seconds;
            Rate = rate;
            Status = status;
            Discontinuity = discontinuity;
            Sequence = sequence;
            IsFallbackActive = isFallbackActive;
            IsPrimaryAvailable = isPrimaryAvailable;
            MusicalPosition = musicalPosition;
        }

        public bool IsComplete =>
            !string.IsNullOrEmpty(TimeDomainId)
            && TimeDomainVersion > 0
            && !string.IsNullOrEmpty(ProviderId)
            && !string.IsNullOrEmpty(TempoMapId)
            && TempoMapVersion > 0
            && Sequence > 0
            && IsFinite(Seconds)
            && IsFinite(Rate)
            && ((Status == FanlightClockStatus.Ready && Rate != 0d)
                || (Status == FanlightClockStatus.Holding && Rate == 0d))
            && MusicalPosition.IsComplete
            && Math.Abs(Seconds - MusicalPosition.Seconds) <= 1e-9;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
