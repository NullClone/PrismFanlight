using System;

namespace PrismFanlight.Core
{
    public enum FanlightClockStatus
    {
        Ready = 0,
        Holding = 1,
        Disconnected = 2,
        Faulted = 3
    }

    public enum FanlightTimeDiscontinuity
    {
        None = 0,
        Seek = 1,
        Loop = 2,
        Reverse = 3,
        AuthorityChanged = 4,
        Reconnected = 5
    }

    public enum FanlightShowTimeFault
    {
        None = 0,
        PrimaryUnavailable = 1,
        InvalidPrimarySample = 2,
        TempoMapUnavailable = 3,
        CoordinatorUnavailable = 4,
        EvaluationOrderInvalid = 5
    }

    public readonly struct FanlightMusicalPosition
    {
        public double Seconds { get; }
        public double Beat { get; }
        public long Bar { get; }
        public double BeatInBar { get; }
        public double BeatPhase { get; }
        public double BarPhase { get; }
        public double Bpm { get; }
        public int BeatsPerBar { get; }
        public int BeatUnit { get; }
        public string TempoSegmentId { get; }

        public FanlightMusicalPosition(
            double seconds,
            double beat,
            long bar,
            double beatInBar,
            double beatPhase,
            double barPhase,
            double bpm,
            int beatsPerBar,
            int beatUnit,
            string tempoSegmentId)
        {
            Seconds = seconds;
            Beat = beat;
            Bar = bar;
            BeatInBar = beatInBar;
            BeatPhase = beatPhase;
            BarPhase = barPhase;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            BeatUnit = beatUnit;
            TempoSegmentId = tempoSegmentId ?? string.Empty;
        }

        public bool IsComplete =>
            IsFinite(Seconds)
            && IsFinite(Beat)
            && IsFinite(BeatInBar)
            && IsFinite(BeatPhase)
            && IsFinite(BarPhase)
            && IsFinite(Bpm)
            && Bpm > 0d
            && BeatsPerBar > 0
            && BeatUnit > 0
            && !string.IsNullOrEmpty(TempoSegmentId);

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public readonly struct FanlightShowTimeSample
    {
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
