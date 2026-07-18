using System;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    public sealed class ConstantTempoMapResolver : IShowTempoMapResolver
    {
        // Fields

        private readonly double _bpm;
        private readonly int _beatsPerBar;
        private readonly int _beatUnit;
        private readonly double _offsetSeconds;


        // Properties

        public string TempoMapId { get; }

        public int Version { get; }


        // Methods

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
