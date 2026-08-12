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
        private const float MinBeatSpacingPixels = 12f;
        private const float MinBarSpacingPixels = 16f;


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

            var director = TimelineEditor.inspectedDirector;
            if (director == null) return;

            var binding = director.GetGenericBinding(tempoTrack);
            var target = binding as PrismFanlight;
            if (target == null && binding is GameObject gameObject) target = gameObject.GetComponent<PrismFanlight>();
            if (target == null || target.TimeManager == null) return;

            if (!tempoTrack.TryBuildRuntimeDefinition(target.TimeManager, out var definition, out _)) return;

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

        private static void DrawTicks(ClipBackgroundRegion region, double rangeStart, double rangeEnd, in FanlightTempoSection section)
        {
            var rangeDuration = rangeEnd - rangeStart;
            if (rangeDuration <= TickTolerance || region.position.width <= 0f) return;

            var pixelsPerSecond = region.position.width / rangeDuration;

            var secondsPerBeat = 60d / section.Bpm;
            var pixelsPerBeat = (float)(secondsPerBeat * pixelsPerSecond);
            var pixelsPerBar = pixelsPerBeat * section.BeatsPerBar;

            if (pixelsPerBar < MinBarSpacingPixels) return;

            var showBeats = pixelsPerBeat >= MinBeatSpacingPixels;

            var beatsPerBar = (double)section.BeatsPerBar;
            var startBeat = EvaluateBeat(rangeStart, section);
            var endBeat = EvaluateBeat(rangeEnd, section);

            double firstTick;
            double lastTick;
            double tickStep;

            if (showBeats)
            {
                tickStep = 1d;
                firstTick = Math.Ceiling(startBeat - TickTolerance);
                lastTick = Math.Ceiling(endBeat - TickTolerance) - 1d;
            }
            else
            {
                tickStep = beatsPerBar;
                firstTick = Math.Ceiling(startBeat / beatsPerBar - TickTolerance) * beatsPerBar;
                lastTick = (Math.Ceiling(endBeat / beatsPerBar - TickTolerance) - 1d) * beatsPerBar;
            }

            if (!IsFinite(firstTick) || !IsFinite(lastTick) || lastTick < firstTick) return;

            var tickCount = (lastTick - firstTick) / tickStep + 1d;
            if (tickCount > MaximumVisibleTickCount) return;

            for (var tick = firstTick; tick <= lastTick;)
            {
                var tickSeconds = section.StartSeconds + (tick - section.StartBeat) * 60d / section.Bpm;

                if (tickSeconds >= rangeStart - TickTolerance && tickSeconds < rangeEnd - TickTolerance)
                {
                    var normalizedTime = (float)((tickSeconds - rangeStart) / rangeDuration);
                    var x = region.position.xMin + region.position.width * Mathf.Clamp01(normalizedTime);
                    var isBar = IsBarTick(tick, section.BeatsPerBar);

                    var width = isBar ? 1.5f : 1f;
                    var color = isBar ? new Color(0.9f, 0.9f, 0.9f, 0.7f) : new Color(0.9f, 0.9f, 0.9f, 0.6f);
                    var height = isBar ? region.position.height * 0.5f : region.position.height * 0.3f;

                    EditorGUI.DrawRect(new Rect(x, region.position.yMin, width, height), color);
                }

                var nextTick = tick + tickStep;
                if (nextTick <= tick) break;

                tick = nextTick;
            }
        }

        private static bool TryGetVisibleSequenceRange(TimelineClip clip, ClipBackgroundRegion region, out double start, out double end)
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

        private static bool TryGetSection(FanlightTempoRuntimeDefinition definition, double seconds, out FanlightTempoSection section)
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

            return Math.Abs(remainder) <= TickTolerance || Math.Abs(Math.Abs(remainder) - beatsPerBar) <= TickTolerance;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
