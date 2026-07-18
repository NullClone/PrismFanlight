using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    public sealed class FanlightTempoMapResolver : IShowTempoMapResolver
    {
        private readonly FanlightTempoSegment[] _segments;

        public FanlightTempoMapResolver(FanlightTempoMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (!map.Validate(out var error)) throw new ArgumentException(error, nameof(map));
            TempoMapId = map.TempoMapId;
            Version = map.Version;
            _segments = map.Segments.ToArray();
        }

        public string TempoMapId { get; }
        public int Version { get; }

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

    public sealed class ConstantTempoMapResolver : IShowTempoMapResolver
    {
        private readonly double _bpm;
        private readonly int _beatsPerBar;
        private readonly int _beatUnit;
        private readonly double _offsetSeconds;

        public ConstantTempoMapResolver(
            string tempoMapId,
            int version,
            double bpm,
            int beatsPerBar,
            int beatUnit = 4,
            double offsetSeconds = 0d)
        {
            TempoMapId = string.IsNullOrEmpty(tempoMapId) ? "tempo.compatibility" : tempoMapId;
            Version = Math.Max(1, version);
            _bpm = Math.Max(1e-6d, bpm);
            _beatsPerBar = Math.Max(1, beatsPerBar);
            _beatUnit = beatUnit is 1 or 2 or 4 or 8 or 16 ? beatUnit : 4;
            _offsetSeconds = offsetSeconds;
        }

        public string TempoMapId { get; }
        public int Version { get; }

        public FanlightMusicalPosition Evaluate(double seconds)
        {
            var beat = (seconds - _offsetSeconds) * _bpm / 60d;
            var barOffset = (long)Math.Floor(beat / _beatsPerBar);
            var beatInBar = PositiveModulo(beat, _beatsPerBar);
            return new FanlightMusicalPosition(
                seconds,
                beat,
                barOffset + 1L,
                beatInBar,
                PositiveModulo(beat, 1d),
                beatInBar / _beatsPerBar,
                _bpm,
                _beatsPerBar,
                _beatUnit,
                "tempo.compatibility.segment");
        }

        private static double PositiveModulo(double value, double divisor)
        {
            var result = value % divisor;
            return result < 0d ? result + divisor : result;
        }
    }
}
