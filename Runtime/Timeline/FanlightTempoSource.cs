using System;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTempoSource
    {
        // Properties

        internal ReadOnlyMemory<double> Starts { get; }

        internal ReadOnlyMemory<double> Ends { get; }

        internal ReadOnlyMemory<double> Bpms { get; }

        internal int BeatsPerBar { get; }

        internal int BeatUnit { get; }

        internal double MusicalOriginSeconds { get; }

        internal bool HasClips => Starts.Length > 0;


        // Methods

        internal FanlightTempoSource(
            ReadOnlyMemory<double> starts,
            ReadOnlyMemory<double> ends,
            ReadOnlyMemory<double> bpms,
            int beatsPerBar,
            int beatUnit,
            double musicalOriginSeconds)
        {
            Starts = starts.Span.ToArray();
            Ends = ends.Span.ToArray();
            Bpms = bpms.Span.ToArray();
            BeatsPerBar = beatsPerBar;
            BeatUnit = beatUnit;
            MusicalOriginSeconds = musicalOriginSeconds;
        }
    }
}
