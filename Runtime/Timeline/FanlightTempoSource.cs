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

        internal bool HasClips => Starts.Length > 0;


        // Methods

        internal FanlightTempoSource(
            ReadOnlyMemory<double> starts,
            ReadOnlyMemory<double> ends,
            ReadOnlyMemory<double> bpms,
            int beatsPerBar,
            int beatUnit)
        {
            Starts = starts.Span.ToArray();
            Ends = ends.Span.ToArray();
            Bpms = bpms.Span.ToArray();
            BeatsPerBar = beatsPerBar;
            BeatUnit = beatUnit;
        }
    }
}
