using System;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal sealed class FanlightTempoScopeResolver
    {
        // Fields

        private readonly double _defaultBpm;
        private readonly int _defaultBeatsPerBar;
        private readonly int _defaultBeatUnit;
        private readonly double _defaultMusicalOriginSeconds;


        // Methods

        internal FanlightTempoScopeResolver(
            double defaultBpm,
            int defaultBeatsPerBar,
            int defaultBeatUnit,
            double defaultMusicalOriginSeconds)
        {
            _defaultBpm = defaultBpm;
            _defaultBeatsPerBar = defaultBeatsPerBar;
            _defaultBeatUnit = defaultBeatUnit;
            _defaultMusicalOriginSeconds = defaultMusicalOriginSeconds;
        }

        internal bool TryResolve(
            in FanlightClockSample clock,
            ReadOnlySpan<FanlightTempoCandidate> candidates,
            out FanlightShowTimeSample sample,
            out FanlightShowTimeFault fault)
        {
            if (!IsValidClock(clock))
            {
                sample = default;
                fault = FanlightShowTimeFault.InvalidPrimarySample;
                return false;
            }

            if (candidates.Length > 1)
            {
                sample = default;
                fault = FanlightShowTimeFault.TempoConflict;
                return false;
            }

            FanlightMusicalPosition musicalPosition;

            if (candidates.Length == 0)
            {
                if (!TryEvaluateDefault(clock.Seconds, out musicalPosition))
                {
                    sample = default;
                    fault = FanlightShowTimeFault.InvalidTempoDefinition;
                    return false;
                }
            }
            else if (!TryEvaluate(candidates[0], out musicalPosition))
            {
                sample = default;
                fault = FanlightShowTimeFault.InvalidTempoDefinition;
                return false;
            }

            sample = new FanlightShowTimeSample(
                clock.Seconds,
                clock.Rate,
                clock.Status,
                clock.Discontinuity,
                clock.IsFallbackActive,
                clock.IsPrimaryAvailable,
                musicalPosition);

            fault = FanlightShowTimeFault.None;

            return true;
        }

        private bool TryEvaluateDefault(double sequenceLocalSeconds, out FanlightMusicalPosition position)
        {
            if (!IsFinite(_defaultBpm)
                || _defaultBpm <= 0d
                || _defaultBeatsPerBar < 1
                || !IsValidBeatUnit(_defaultBeatUnit)
                || !IsFinite(_defaultMusicalOriginSeconds))
            {
                position = default;
                return false;
            }

            var beat = (sequenceLocalSeconds - _defaultMusicalOriginSeconds) * _defaultBpm / 60d;
            var barOffset = (long)Math.Floor(beat / _defaultBeatsPerBar);
            var beatInBar = PositiveModulo(beat, _defaultBeatsPerBar);
            position = CreatePosition(
                sequenceLocalSeconds,
                beat,
                barOffset + 1L,
                beatInBar,
                _defaultBpm,
                _defaultBeatsPerBar,
                _defaultBeatUnit);
            return position.IsComplete;
        }

        private static bool TryEvaluate(in FanlightTempoCandidate candidate, out FanlightMusicalPosition position)
        {
            position = default;

            if (!IsFinite(candidate.SequenceLocalSeconds)
                || candidate.Definition == null
                || !TryValidateDefinition(candidate.Definition.Sections.Span))
            {
                return false;
            }

            var sections = candidate.Definition.Sections.Span;

            if (candidate.SequenceLocalSeconds < sections[0].StartSeconds
                || candidate.SequenceLocalSeconds > sections[^1].EndSeconds)
            {
                return false;
            }

            var index = FindSection(sections, candidate.SequenceLocalSeconds);
            var section = sections[index];
            var beat = section.StartBeat
                       + (candidate.SequenceLocalSeconds - section.StartSeconds) * section.Bpm / 60d;
            var relativeBeatInBar = section.StartBeatInBar + beat - section.StartBeat;
            var completedBars = (long)Math.Floor(relativeBeatInBar / section.BeatsPerBar);
            var beatInBar = PositiveModulo(relativeBeatInBar, section.BeatsPerBar);
            position = CreatePosition(
                candidate.SequenceLocalSeconds,
                beat,
                section.StartBar + completedBars,
                beatInBar,
                section.Bpm,
                section.BeatsPerBar,
                section.BeatUnit);

            return position.IsComplete;
        }

        private static bool TryValidateDefinition(ReadOnlySpan<FanlightTempoSection> sections)
        {
            if (sections.Length == 0 || sections[0].StartSeconds != 0d) return false;

            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];

                if (!IsFinite(section.StartSeconds)
                    || !IsFinite(section.StartBeat)
                    || !IsFinite(section.StartBeatInBar)
                    || !IsFinite(section.Bpm)
                    || section.Bpm <= 0d
                    || section.BeatsPerBar < 1
                    || !IsValidBeatUnit(section.BeatUnit)
                    || section.StartBeatInBar < 0d
                    || section.StartBeatInBar >= section.BeatsPerBar
                    || double.IsNaN(section.EndSeconds)
                    || double.IsNegativeInfinity(section.EndSeconds)
                    || section.EndSeconds <= section.StartSeconds)
                {
                    return false;
                }

                if (i < sections.Length - 1 && section.EndSeconds != sections[i + 1].StartSeconds)
                {
                    return false;
                }

                if (i < sections.Length - 1)
                {
                    var next = sections[i + 1];
                    var expectedBeat = section.StartBeat
                                       + (next.StartSeconds - section.StartSeconds) * section.Bpm / 60d;
                    var relativeBeatInBar = section.StartBeatInBar + expectedBeat - section.StartBeat;
                    var completedBars = (long)Math.Floor(relativeBeatInBar / section.BeatsPerBar);
                    var expectedBar = section.StartBar + completedBars;
                    var expectedBeatInBar = PositiveModulo(relativeBeatInBar, section.BeatsPerBar);
                    var changesSignature = next.BeatsPerBar != section.BeatsPerBar
                                           || next.BeatUnit != section.BeatUnit;

                    if (Math.Abs(next.StartBeat - expectedBeat) > 1e-6d
                        || next.StartBar != expectedBar
                        || changesSignature && Math.Abs(expectedBeatInBar) > 1e-6d
                        || Math.Abs(next.StartBeatInBar - (changesSignature ? 0d : expectedBeatInBar)) > 1e-6d)
                    {
                        return false;
                    }
                }
            }

            return double.IsPositiveInfinity(sections[^1].EndSeconds);
        }

        private static int FindSection(ReadOnlySpan<FanlightTempoSection> sections, double seconds)
        {
            var low = 0;
            var high = sections.Length - 1;

            while (low <= high)
            {
                var middle = (low + high) >> 1;
                if (sections[middle].StartSeconds <= seconds) low = middle + 1;
                else high = middle - 1;
            }

            return Math.Max(0, high);
        }

        private static FanlightMusicalPosition CreatePosition(
            double sequenceLocalSeconds,
            double beat,
            long bar,
            double beatInBar,
            double bpm,
            int beatsPerBar,
            int beatUnit)
        {
            return new FanlightMusicalPosition(
                sequenceLocalSeconds,
                beat,
                bar,
                beatInBar,
                PositiveModulo(beat, 1d),
                beatInBar / beatsPerBar,
                bpm,
                beatsPerBar,
                beatUnit);
        }

        private static bool IsValidClock(in FanlightClockSample clock)
        {
            return IsFinite(clock.Seconds)
                   && IsFinite(clock.Rate)
                   && ((clock.Status == FanlightClockStatus.Ready && clock.Rate != 0d)
                       || (clock.Status == FanlightClockStatus.Holding && clock.Rate == 0d));
        }

        private static bool IsValidBeatUnit(int beatUnit) => beatUnit is 1 or 2 or 4 or 8 or 16;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static double PositiveModulo(double value, double divisor)
        {
            var result = value % divisor;
            return result < 0d ? result + divisor : result;
        }
    }
}
