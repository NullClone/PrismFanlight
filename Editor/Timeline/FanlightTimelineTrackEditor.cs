using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTimelineTrackAsset))]
    internal sealed class FanlightTimelineTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);

            if (track is not FanlightTimelineTrackAsset fanlightTrack) return options;

            var errors = new List<string>();

            CollectStructuralErrors(fanlightTrack, errors);

            var target = ResolveBinding(binding);

            if (binding == null)
            {
                errors.Add("Track binding is missing. Bind this track to a PrismFanlight component.");
            }
            else if (target == null)
            {
                errors.Add("Track binding has the wrong type. Bind this track to a PrismFanlight component.");
            }

            if (target != null)
            {
                CollectColorBlockErrors(fanlightTrack, target.LayoutAsset, errors);
                CollectTempoRequirementErrors(fanlightTrack, target, errors);

                FanlightControlBindingValidator.CollectErrors(
                    fanlightTrack.timelineAsset,
                    TimelineEditor.inspectedDirector,
                    target,
                    errors);
            }

            options.errorText = AppendErrors(options.errorText, errors);
            return options;
        }

        internal static string GetClipError(TimelineClip clip)
        {
            if (clip == null || clip.GetParentTrack() is not FanlightTimelineTrackAsset track) return string.Empty;

            var expectedClipType = GetExpectedClipType(track);

            if (clip.asset == null || expectedClipType == null || !expectedClipType.IsInstanceOfType(clip.asset))
            {
                var expectedName = expectedClipType == null ? "the track's typed clip" : expectedClipType.Name;
                return $"This clip does not match {track.GetType().Name}. Replace it with {expectedName}.";
            }

            try
            {
                _ = ((FanlightTimelineClipAsset)clip.asset).Value;
            }
            catch (ArgumentException exception)
            {
                return exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                return exception.Message;
            }

            var clips = GetClips(track);

            if (TryFindTripleOverlap(clips, out var overlapStart))
            {
                return $"Three or more clips overlap at {overlapStart:0.###} seconds, including extrapolation. Move or trim clips so at most two are active.";
            }

            if (TryFindDiscreteConflict(track, clips, clip, out var conflictStart, out var fieldName))
            {
                return $"Clips starting at {conflictStart:0.###} seconds assign different {fieldName} values. Change one value or one start time.";
            }

            return string.Empty;
        }

        internal static string AppendError(string current, string error)
        {
            if (string.IsNullOrEmpty(error)) return current;

            return string.IsNullOrEmpty(current) ? error : $"{current}\n{error}";
        }

        private static void CollectStructuralErrors(FanlightTimelineTrackAsset track, List<string> errors)
        {
            if (!FanlightTimelinePatchMixer.HasFields(track.PatchKind, track.FieldMask))
            {
                errors.Add("Field Mask is None. Select at least one field on this track.");
            }

            var expectedClipType = GetExpectedClipType(track);
            var clips = GetClips(track);
            var clipValuesValid = true;

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];

                if (clip.asset == null || expectedClipType == null || !expectedClipType.IsInstanceOfType(clip.asset))
                {
                    var actualName = clip.asset == null ? "missing asset" : clip.asset.GetType().Name;
                    var expectedName = expectedClipType == null ? "the track's typed clip" : expectedClipType.Name;
                    errors.Add($"Clip '{clip.displayName}' uses {actualName}. Replace it with {expectedName}.");
                    clipValuesValid = false;
                    continue;
                }

                try
                {
                    _ = ((FanlightTimelineClipAsset)clip.asset).Value;
                }
                catch (ArgumentException exception)
                {
                    errors.Add($"Clip '{clip.displayName}': {exception.Message}");
                    clipValuesValid = false;
                }
                catch (InvalidOperationException exception)
                {
                    errors.Add($"Clip '{clip.displayName}': {exception.Message}");
                    clipValuesValid = false;
                }
            }

            if (TryFindTripleOverlap(clips, out var overlapStart))
            {
                errors.Add($"Three or more clips overlap at {overlapStart:0.###} seconds, including extrapolation. Move or trim clips so at most two are active.");
            }

            if (clipValuesValid
                && TryFindDiscreteConflict(track, clips, null, out var conflictStart, out var fieldName))
            {
                errors.Add($"Clips starting at {conflictStart:0.###} seconds assign different {fieldName} values. Change one value or one start time.");
            }

            if (track.timelineAsset != null)
            {
                if (track is FanlightMotionTrack && CountTracks<FanlightMotionTrack>(track.timelineAsset) > 1)
                {
                    errors.Add("A Timeline Asset can contain only one Prism Fanlight Motion Track.");
                }

                if (track is FanlightColorTrack && CountTracks<FanlightColorTrack>(track.timelineAsset) > 1)
                {
                    errors.Add("A Timeline Asset can contain only one Prism Fanlight Color Track.");
                }

                if (track is FanlightIntensityTrack && CountTracks<FanlightIntensityTrack>(track.timelineAsset) > 1)
                {
                    errors.Add("A Timeline Asset can contain only one Prism Fanlight Intensity Track.");
                }
            }
        }

        private static int CountTracks<T>(TimelineAsset timelineAsset) where T : TrackAsset
        {
            var count = 0;
            foreach (var outputTrack in timelineAsset.GetOutputTracks())
            {
                if (outputTrack is T) count++;
            }

            return count;
        }

        private static void CollectColorBlockErrors(FanlightTimelineTrackAsset track, FanlightLayoutAsset layout, List<string> errors)
        {
            if (track is not FanlightColorTrack) return;

            foreach (var clip in track.GetClips())
            {
                if (clip.asset is not FanlightColorClip colorClip) continue;

                FanlightColorState color;

                try
                {
                    color = colorClip.Value.Color;
                }
                catch (ArgumentException exception)
                {
                    errors.Add($"Clip '{clip.displayName}': {exception.Message}");
                    continue;
                }
                catch (InvalidOperationException exception)
                {
                    errors.Add($"Clip '{clip.displayName}': {exception.Message}");
                    continue;
                }

                for (var sourceIndex = 0; sourceIndex < 3; sourceIndex++)
                {
                    if (color.GetSourceWeight(sourceIndex) <= 0f) continue;
                    var source = color.GetSource(sourceIndex);
                    if (source.Mode != FanlightColorMode.BlockPalette) continue;

                    if (layout == null || !layout.IsInitialized)
                    {
                        errors.Add($"Clip '{clip.displayName}' uses Block Palette but the binding has no initialized Layout.");
                        continue;
                    }

                    if (!IsCompleteBlockPalette(source, layout))
                    {
                        errors.Add($"Clip '{clip.displayName}' must map every active Layout Block exactly once by Stable Block ID.");
                    }
                }
            }
        }

        private static void CollectTempoRequirementErrors(
            FanlightTimelineTrackAsset track,
            PrismFanlight target,
            List<string> errors)
        {
            if (track.timelineAsset == null || CountTracks<FanlightTempoTrack>(track.timelineAsset) > 0) return;

            if (track is FanlightMotionTrack)
            {
                errors.Add("A song Timeline containing a Motion Track requires one Fanlight Tempo Track.");
                return;
            }

            if (track is not FanlightIntensityTrack) return;

            if (target.BaseState.Intensity.HasDynamicMask())
            {
                errors.Add("A song Timeline using Pulse or Traveling Wave requires one Fanlight Tempo Track.");
                return;
            }

            foreach (var clip in track.GetClips())
            {
                if (clip.asset is not FanlightIntensityClip intensityClip) continue;

                try
                {
                    if (intensityClip.Value.Intensity.HasDynamicMask())
                    {
                        errors.Add("A song Timeline using Pulse or Traveling Wave requires one Fanlight Tempo Track.");
                        return;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
            }
        }

        private static bool IsCompleteBlockPalette(FanlightColorSource source, FanlightLayoutAsset layout)
        {
            if (source.BlockPaletteEntryCount != layout.BlockCount) return false;

            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (var entryIndex = 0; entryIndex < source.BlockPaletteEntryCount; entryIndex++)
            {
                var entry = source.GetBlockPaletteEntry(entryIndex);
                if (!ids.Add(entry.StableBlockId)) return false;
            }

            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                if (!ids.Contains(layout.GetBlock(blockIndex).BlockId)) return false;
            }

            return true;
        }

        private static List<TimelineClip> GetClips(FanlightTimelineTrackAsset track)
        {
            var clips = new List<TimelineClip>();

            foreach (var clip in track.GetClips())
            {
                clips.Add(clip);
            }

            return clips;
        }

        private static Type GetExpectedClipType(FanlightTimelineTrackAsset track)
        {
            var attributes = track.GetType().GetCustomAttributes(typeof(TrackClipTypeAttribute), true);

            for (var i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] is TrackClipTypeAttribute attribute) return attribute.inspectedType;
            }

            return null;
        }

        private static bool TryFindTripleOverlap(List<TimelineClip> clips, out double overlapStart)
        {
            for (var i = 0; i < clips.Count; i++)
            {
                var candidate = clips[i].extrapolatedStart;
                var activeCount = 0;

                for (var j = 0; j < clips.Count; j++)
                {
                    var start = clips[j].extrapolatedStart;
                    var end = start + clips[j].extrapolatedDuration;

                    if (start <= candidate && candidate < end) activeCount++;
                }

                if (activeCount < 3) continue;

                overlapStart = candidate;
                return true;
            }

            overlapStart = 0d;
            return false;
        }

        private static bool TryFindDiscreteConflict(
            FanlightTimelineTrackAsset track,
            List<TimelineClip> clips,
            TimelineClip requiredClip,
            out double conflictStart,
            out string fieldName)
        {
            for (var i = 0; i < clips.Count - 1; i++)
            {
                if (clips[i].asset is not FanlightTimelineClipAsset leftAsset) continue;

                for (var j = i + 1; j < clips.Count; j++)
                {
                    if (clips[i].start != clips[j].start) continue;
                    if (requiredClip != null && clips[i] != requiredClip && clips[j] != requiredClip) continue;
                    if (clips[j].asset is not FanlightTimelineClipAsset rightAsset) continue;

                    if (!TryGetDiscreteConflict(
                            track.PatchKind,
                            track.FieldMask,
                            leftAsset.Value,
                            rightAsset.Value,
                            out fieldName)) continue;

                    conflictStart = clips[i].start;
                    return true;
                }
            }

            conflictStart = 0d;
            fieldName = string.Empty;
            return false;
        }

        private static bool TryGetDiscreteConflict(
            FanlightTimelinePatchKind kind,
            FanlightTimelineFieldMask fieldMask,
            FanlightTimelineClipValue left,
            FanlightTimelineClipValue right,
            out string fieldName)
        {
            fieldName = string.Empty;

            if (kind == FanlightTimelinePatchKind.Direction
                && (fieldMask.Direction & FanlightDirectionFields.Mode) != 0
                && left.Direction.Mode != right.Direction.Mode)
            {
                fieldName = "Mode";
                return true;
            }

            return false;
        }

        private static PrismFanlight ResolveBinding(Object binding)
        {
            if (binding is PrismFanlight target) return target;
            return binding is GameObject gameObject ? gameObject.GetComponent<PrismFanlight>() : null;
        }

        private static string AppendErrors(string current, List<string> errors)
        {
            for (var i = 0; i < errors.Count; i++)
            {
                current = AppendError(current, errors[i]);
            }

            return current;
        }
    }
}
