namespace PrismFanlight.Core
{
    public readonly struct FanlightMusicalPosition
    {
        // Properties

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


        // Methods

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
}
