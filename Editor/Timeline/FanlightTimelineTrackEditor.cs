using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTimelineTrackAsset))]
    internal sealed class FanlightTimelineTrackEditor : TrackEditor
    {
        // Methods

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
            else if (HasMultipleDirectorBindings(target))
            {
                errors.Add($"Multiple active PlayableDirectors bind PrismFanlight '{target.name}'. Disable or rebind all but one director.");
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

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];

                if (clip.asset != null && expectedClipType != null && expectedClipType.IsInstanceOfType(clip.asset)) continue;

                var actualName = clip.asset == null ? "missing asset" : clip.asset.GetType().Name;
                var expectedName = expectedClipType == null ? "the track's typed clip" : expectedClipType.Name;
                errors.Add($"Clip '{clip.displayName}' uses {actualName}. Replace it with {expectedName}.");
                break;
            }

            if (TryFindTripleOverlap(clips, out var overlapStart))
            {
                errors.Add($"Three or more clips overlap at {overlapStart:0.###} seconds, including extrapolation. Move or trim clips so at most two are active.");
            }

            if (TryFindDiscreteConflict(track, clips, null, out var conflictStart, out var fieldName))
            {
                errors.Add($"Clips starting at {conflictStart:0.###} seconds assign different {fieldName} values. Change one value or one start time.");
            }
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

            if (kind == FanlightTimelinePatchKind.Pose
                && (fieldMask.Pose & FanlightPoseFields.HandZone) != 0
                && left.Pose.HandZone != right.Pose.HandZone)
            {
                fieldName = "Hand Zone";
                return true;
            }

            if (kind == FanlightTimelinePatchKind.Noise
                && (fieldMask.Noise & FanlightNoiseFields.Octaves) != 0
                && left.Noise.Octaves != right.Noise.Octaves)
            {
                fieldName = "Octaves";
                return true;
            }

            if (kind == FanlightTimelinePatchKind.Direction
                && (fieldMask.Direction & FanlightDirectionFields.Mode) != 0
                && left.Direction.Mode != right.Direction.Mode)
            {
                fieldName = "Mode";
                return true;
            }

            if (kind == FanlightTimelinePatchKind.Visibility
                && (fieldMask.Visibility & FanlightVisibilityFields.PenlightsEnabled) != 0
                && left.Visibility.PenlightsEnabled != right.Visibility.PenlightsEnabled)
            {
                fieldName = "Penlights Enabled";
                return true;
            }

            if (kind == FanlightTimelinePatchKind.Visibility
                && (fieldMask.Visibility & FanlightVisibilityFields.AudienceBodiesEnabled) != 0
                && left.Visibility.AudienceBodiesEnabled != right.Visibility.AudienceBodiesEnabled)
            {
                fieldName = "Audience Bodies Enabled";
                return true;
            }

            return false;
        }

        private static PrismFanlight ResolveBinding(Object binding)
        {
            if (binding is PrismFanlight target) return target;
            return binding is GameObject gameObject ? gameObject.GetComponent<PrismFanlight>() : null;
        }

        private static bool HasMultipleDirectorBindings(PrismFanlight target)
        {
            var bindingCount = 0;
            var directors = Resources.FindObjectsOfTypeAll<PlayableDirector>();

            for (var i = 0; i < directors.Length; i++)
            {
                var director = directors[i];

                if (director == null
                    || !director.enabled
                    || !director.gameObject.activeInHierarchy
                    || !director.gameObject.scene.IsValid()
                    || director.playableAsset is not TimelineAsset timelineAsset)
                {
                    continue;
                }

                var bindsTarget = false;

                foreach (var outputTrack in timelineAsset.GetOutputTracks())
                {
                    if (outputTrack is not FanlightTimelineTrackAsset) continue;
                    if (ResolveBinding(director.GetGenericBinding(outputTrack)) != target) continue;

                    bindsTarget = true;
                    break;
                }

                if (!bindsTarget) continue;

                bindingCount++;
                if (bindingCount > 1) return true;
            }

            return false;
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
