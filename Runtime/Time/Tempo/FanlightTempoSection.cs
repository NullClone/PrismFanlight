namespace PrismFanlight.Time
{
    internal readonly struct FanlightTempoSection
    {
        // Properties

        internal double StartSeconds { get; }

        internal double EndSeconds { get; }

        internal double StartBeat { get; }

        internal long StartBar { get; }

        internal double StartBeatInBar { get; }

        internal double Bpm { get; }

        internal int BeatsPerBar { get; }

        internal int BeatUnit { get; }


        // Methods

        internal FanlightTempoSection(
            double startSeconds,
            double endSeconds,
            double startBeat,
            long startBar,
            double startBeatInBar,
            double bpm,
            int beatsPerBar,
            int beatUnit)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            StartBeat = startBeat;
            StartBar = startBar;
            StartBeatInBar = startBeatInBar;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            BeatUnit = beatUnit;
        }
    }
}
