namespace PrismFanlight.Core
{
    internal readonly struct FanlightMusicalPosition
    {
        // Properties

        internal double SequenceLocalSeconds { get; }

        internal double Beat { get; }

        internal long Bar { get; }

        internal double BeatInBar { get; }

        internal double BeatPhase { get; }

        internal double BarPhase { get; }

        internal double Bpm { get; }

        internal int BeatsPerBar { get; }

        internal int BeatUnit { get; }

        // Methods

        internal FanlightMusicalPosition(
            double sequenceLocalSeconds,
            double beat,
            long bar,
            double beatInBar,
            double beatPhase,
            double barPhase,
            double bpm,
            int beatsPerBar,
            int beatUnit)
        {
            SequenceLocalSeconds = sequenceLocalSeconds;
            Beat = beat;
            Bar = bar;
            BeatInBar = beatInBar;
            BeatPhase = beatPhase;
            BarPhase = barPhase;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            BeatUnit = beatUnit;
        }

        internal bool IsComplete =>
            IsFinite(SequenceLocalSeconds)
            && IsFinite(Beat)
            && IsFinite(BeatInBar)
            && IsFinite(BeatPhase)
            && IsFinite(BarPhase)
            && IsFinite(Bpm)
            && Bpm > 0d
            && BeatsPerBar > 0
            && BeatUnit is 1 or 2 or 4 or 8 or 16;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
