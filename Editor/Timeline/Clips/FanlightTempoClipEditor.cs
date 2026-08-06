using System;
using PrismFanlight.Time;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTempoClip))]
    internal sealed class FanlightTempoClipEditor : ClipEditor
    {
        // Fields

        private const double TickTolerance = 1e-6d;
        private const int MaximumVisibleTickCount = 512;

        private static readonly Color BeatTickColor = new(0.18f, 0.82f, 1.0f, 0.54f);
        private static readonly Color BarTickColor = new(1.0f, 0.86f, 0.28f, 0.72f);


        // Methods

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);

            if (clip.asset is not FanlightTempoClip tempoClip) return options;

            options.highlightColor = new Color(0.18f, 0.55f, 0.85f, 1f);
            options.tooltip = $"{tempoClip.Bpm:0.###} BPM";

            if (!tempoClip.TryValidate(out var error))
            {
                options.errorText = error;
            }

            return options;
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            UpdateDisplayName(clip);
        }

        public override void OnClipChanged(TimelineClip clip)
        {
            UpdateDisplayName(clip);
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);

            if (clip.asset is not FanlightTempoClip) return;
            if (clip.GetParentTrack() is not FanlightTempoTrack tempoTrack) return;

            if (!tempoTrack.TryBuildRuntimeDefinition(out var definition, out _)) return;

            if (!TryGetVisibleSequenceRange(clip, region, out var visibleStart, out var visibleEnd)) return;
            if (!TryGetSection(definition, clip.start, out var section)) return;

            var rangeStart = Math.Max(visibleStart, clip.start);
            var rangeEnd = Math.Min(visibleEnd, clip.end);

            if (rangeEnd <= rangeStart + TickTolerance) return;

            DrawTicks(region, rangeStart, rangeEnd, section);
        }


        private static void UpdateDisplayName(TimelineClip clip)
        {
            if (clip.asset is FanlightTempoClip tempoClip)
            {
                clip.displayName = $"{tempoClip.Bpm:0.###} BPM";
            }
        }

        private static void DrawTicks(
            ClipBackgroundRegion region,
            double rangeStart,
            double rangeEnd,
            in FanlightTempoSection section)
        {
            var startBeat = EvaluateBeat(rangeStart, section);
            var endBeat = EvaluateBeat(rangeEnd, section);
            var firstTick = Math.Ceiling(startBeat - TickTolerance);
            var lastTick = Math.Ceiling(endBeat - TickTolerance) - 1d;

            if (!IsFinite(firstTick) || !IsFinite(lastTick) || lastTick < firstTick) return;

            var tickStep = 1d;
            var tickCount = lastTick - firstTick + 1d;

            if (tickCount > MaximumVisibleTickCount)
            {
                firstTick = Math.Ceiling(startBeat / section.BeatsPerBar - TickTolerance) * section.BeatsPerBar;
                lastTick = (Math.Ceiling(endBeat / section.BeatsPerBar - TickTolerance) - 1d) * section.BeatsPerBar;
                tickStep = section.BeatsPerBar;
                tickCount = (lastTick - firstTick) / tickStep + 1d;
            }

            if (!IsFinite(firstTick) || !IsFinite(lastTick) || lastTick < firstTick) return;

            if (tickCount > MaximumVisibleTickCount)
            {
                var barStride = Math.Ceiling(tickCount / MaximumVisibleTickCount);
                tickStep *= barStride;
            }

            for (var tick = firstTick; tick <= lastTick;)
            {
                var tickSeconds = section.StartSeconds + (tick - section.StartBeat) * 60d / section.Bpm;

                if (tickSeconds >= rangeStart - TickTolerance && tickSeconds < rangeEnd - TickTolerance)
                {
                    var normalizedTime = (float)((tickSeconds - rangeStart) / (rangeEnd - rangeStart));
                    var x = region.position.xMin + region.position.width * Mathf.Clamp01(normalizedTime);
                    var isBar = IsBarTick(tick, section.BeatsPerBar);
                    var width = isBar ? 2f : 1f;
                    var color = isBar ? BarTickColor : BeatTickColor;
                    var height = isBar ? region.position.height * 0.7f : region.position.height * 0.5f;

                    EditorGUI.DrawRect(new Rect(x, region.position.yMin, width, height), color);
                }

                var nextTick = tick + tickStep;

                if (nextTick <= tick) break;

                tick = nextTick;
            }
        }

        private static bool TryGetVisibleSequenceRange(
            TimelineClip clip,
            ClipBackgroundRegion region,
            out double start,
            out double end)
        {
            var timeScale = clip.timeScale;

            if (!IsFinite(timeScale) || timeScale <= 0d)
            {
                start = 0;
                end = 0;
                return false;
            }

            start = clip.start + (region.startTime - clip.clipIn) / timeScale;
            end = clip.start + (region.endTime - clip.clipIn) / timeScale;
            return IsFinite(start) && IsFinite(end) && end > start;
        }

        private static bool TryGetSection(
            FanlightTempoRuntimeDefinition definition,
            double seconds,
            out FanlightTempoSection section)
        {
            section = default;

            if (definition == null || !IsFinite(seconds)) return false;

            var sections = definition.Sections.Span;
            var found = false;

            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i].StartSeconds > seconds + TickTolerance) break;

                section = sections[i];
                found = true;
            }

            return found && seconds < section.EndSeconds + TickTolerance;
        }

        private static double EvaluateBeat(double seconds, in FanlightTempoSection section)
        {
            return section.StartBeat + (seconds - section.StartSeconds) * section.Bpm / 60d;
        }

        private static bool IsBarTick(double beat, int beatsPerBar)
        {
            var remainder = beat % beatsPerBar;
            return Math.Abs(remainder) <= TickTolerance
                   || Math.Abs(Math.Abs(remainder) - beatsPerBar) <= TickTolerance;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
