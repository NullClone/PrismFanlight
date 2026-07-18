using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    public sealed class FanlightTempoMapResolver : IShowTempoMapResolver
    {
        // Fields

        private readonly FanlightTempoSegment[] _segments;


        // Properties

        public string TempoMapId { get; }

        public int Version { get; }


        // Methods

        public FanlightTempoMapResolver(FanlightTempoMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            if (!map.Validate(out var error)) throw new ArgumentException(error, nameof(map));

            TempoMapId = map.TempoMapId;
            Version = map.Version;
            _segments = map.Segments.ToArray();
        }

        public FanlightMusicalPosition Evaluate(double seconds)
        {
            var index = FindSegment(seconds);
            var segment = _segments[index];
            var beat = segment.StartBeat + (seconds - segment.StartSeconds) * segment.Bpm / 60d;
            var relativeBeat = beat - segment.StartBeat;
            var completedBars = (long)Math.Floor(relativeBeat / segment.BeatsPerBar);
            var beatInBar = PositiveModulo(relativeBeat, segment.BeatsPerBar);

            return new FanlightMusicalPosition(
                seconds,
                beat,
                segment.StartBar + completedBars,
                beatInBar,
                PositiveModulo(beat, 1d),
                beatInBar / segment.BeatsPerBar,
                segment.Bpm,
                segment.BeatsPerBar,
                segment.BeatUnit,
                segment.SegmentId);
        }

        private int FindSegment(double seconds)
        {
            var low = 0;
            var high = _segments.Length - 1;
            while (low <= high)
            {
                var middle = (low + high) >> 1;
                if (_segments[middle].StartSeconds <= seconds) low = middle + 1;
                else high = middle - 1;
            }

            return Math.Max(0, high);
        }

        private static double PositiveModulo(double value, double divisor)
        {
            var result = value % divisor;
            return result < 0d ? result + divisor : result;
        }
    }
}
