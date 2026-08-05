using System;
using System.Collections.Generic;
using PrismFanlight.Time;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTempoDefinitionBuilder
    {
        // Fields

        private const double BoundaryTolerance = 1e-6d;


        // Methods

        internal static bool TryBuild(
            double defaultBpm,
            int beatsPerBar,
            int beatUnit,
            double musicalOriginSeconds,
            IEnumerable<TimelineClip> sourceClips,
            out FanlightTempoRuntimeDefinition definition,
            out string error)
        {
            definition = null;

            if (!IsFinite(defaultBpm) || defaultBpm <= 0d)
            {
                error = "Tempo Track Default BPM must be a finite value greater than zero.";
                return false;
            }

            if (beatsPerBar < 1 || !IsValidBeatUnit(beatUnit))
            {
                error = "Tempo Track time signature is invalid.";
                return false;
            }

            if (!IsFinite(musicalOriginSeconds))
            {
                error = "Tempo Track Musical Origin Seconds must be finite.";
                return false;
            }

            var clips = new List<TimelineClip>();

            if (sourceClips != null)
            {
                foreach (var clip in sourceClips)
                {
                    if (clip != null) clips.Add(clip);
                }
            }

            clips.Sort(CompareClips);

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];

                if (clip.asset is not FanlightTempoClip tempoClip)
                {
                    error = "Tempo Track contains an unsupported Clip type.";
                    return false;
                }

                if (!tempoClip.TryValidate(out error)) return false;

                if (!IsFinite(clip.start) || clip.start < 0d || !IsFinite(clip.duration) || clip.duration <= 0d)
                {
                    error = "Tempo Section Clip time and duration must be finite, with a positive duration.";
                    return false;
                }

                if (i > 0 && clip.start < clips[i - 1].end - BoundaryTolerance)
                {
                    error = $"Tempo Section Clips overlap at {clip.start:0.###} seconds.";
                    return false;
                }
            }

            var starts = new List<double>();
            var ends = new List<double>();
            var bpms = new List<double>();
            var cursor = 0d;

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                var start = clip.start <= cursor + BoundaryTolerance ? cursor : clip.start;

                if (clip.end <= start)
                {
                    error = "Tempo Section Clip becomes empty after Timeline boundary normalization.";
                    return false;
                }

                if (start > cursor)
                {
                    AddSection(cursor, start, defaultBpm, starts, ends, bpms);
                }

                AddSection(start, clip.end, ((FanlightTempoClip)clip.asset).Bpm, starts, ends, bpms);
                cursor = clip.end;
            }

            AddSection(cursor, double.PositiveInfinity, defaultBpm, starts, ends, bpms);

            var rawBeats = new double[starts.Count];

            for (var i = 1; i < starts.Count; i++)
            {
                rawBeats[i] = rawBeats[i - 1]
                              + (starts[i] - starts[i - 1]) * bpms[i - 1] / 60d;
            }

            var originBeat = EvaluateRawBeat(musicalOriginSeconds, starts, bpms, rawBeats, defaultBpm);
            var sections = new FanlightTempoSection[starts.Count];

            for (var i = 0; i < starts.Count; i++)
            {
                var startBeat = rawBeats[i] - originBeat;
                var startBeatInBar = PositiveModulo(startBeat, beatsPerBar);
                var startBar = (long)Math.Floor(startBeat / beatsPerBar) + 1L;
                sections[i] = new FanlightTempoSection(
                    starts[i],
                    ends[i],
                    startBeat,
                    startBar,
                    startBeatInBar,
                    bpms[i],
                    beatsPerBar,
                    beatUnit);
            }

            definition = new FanlightTempoRuntimeDefinition(sections);
            error = string.Empty;
            return true;
        }

        private static void AddSection(
            double start,
            double end,
            double bpm,
            List<double> starts,
            List<double> ends,
            List<double> bpms)
        {
            starts.Add(start);
            ends.Add(end);
            bpms.Add(bpm);
        }

        private static double EvaluateRawBeat(
            double seconds,
            List<double> starts,
            List<double> bpms,
            double[] rawBeats,
            double defaultBpm)
        {
            if (seconds < 0d) return seconds * defaultBpm / 60d;

            var index = FindSection(seconds, starts);
            return rawBeats[index] + (seconds - starts[index]) * bpms[index] / 60d;
        }

        private static int FindSection(double seconds, List<double> starts)
        {
            var low = 0;
            var high = starts.Count - 1;

            while (low <= high)
            {
                var middle = (low + high) >> 1;
                if (starts[middle] <= seconds) low = middle + 1;
                else high = middle - 1;
            }

            return Math.Max(0, high);
        }

        private static int CompareClips(TimelineClip left, TimelineClip right)
        {
            return left.start.CompareTo(right.start);
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
