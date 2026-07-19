using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineTrackAsset : TrackAsset
    {
        // Fields

        [SerializeField, HideInInspector]
        private string _stableTrackId = string.Empty;

        [SerializeField]
        private int _priority;


        // Properties

        internal abstract FanlightTimelinePatchKind PatchKind { get; }

        internal abstract FanlightTimelineFieldMask FieldMask { get; }


        // Methods

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            ValidateRuntimeIdentities();

            var ranges = new FanlightTimelineClipRange[inputCount];
            var starts = new double[inputCount];
            var ends = new double[inputCount];
            var stableClipIds = new string[inputCount];
            var index = 0;

            foreach (var clip in GetClips())
            {
                if (index >= inputCount) break;

                if (clip.asset is not FanlightTimelineClipAsset asset)
                {
                    throw new InvalidOperationException("Typed Timeline tracks require typed clips.");
                }

                stableClipIds[index] = asset.StableClipId;
                starts[index] = clip.start;
                ends[index] = clip.end;
                index++;
            }

            for (var i = 0; i < index; i++)
            {
                var holdEnd = i + 1 < index ? Math.Max(ends[i], starts[i + 1]) : double.PositiveInfinity;
                ranges[i] = new FanlightTimelineClipRange(stableClipIds[i], ends[i], holdEnd);
            }

            var mixer = ScriptPlayable<FanlightTimelineMixerBehaviour>.Create(graph, inputCount);
            mixer.GetBehaviour().Configure(
                $"timeline:{_stableTrackId}",
                PatchKind,
                FieldMask,
                _priority,
                ranges);

            return mixer;
        }

        private void ValidateRuntimeIdentities()
        {
            var usedTrackIds = new HashSet<string>(StringComparer.Ordinal);
            var usedClipIds = new HashSet<string>(StringComparer.Ordinal);

            if (timelineAsset == null)
            {
                ValidateRuntimeTrackIdentity(this, usedTrackIds);
                ValidateRuntimeClipIdentities(this, usedClipIds);
                return;
            }

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is not FanlightTimelineTrackAsset typed) continue;

                ValidateRuntimeTrackIdentity(typed, usedTrackIds);
                ValidateRuntimeClipIdentities(typed, usedClipIds);
            }
        }

        private static void ValidateRuntimeTrackIdentity(
            FanlightTimelineTrackAsset track,
            HashSet<string> usedTrackIds)
        {
            if (string.IsNullOrWhiteSpace(track._stableTrackId))
            {
                throw new InvalidOperationException("Timeline Track Stable ID must be assigned during authoring.");
            }

            if (!usedTrackIds.Add(track._stableTrackId))
            {
                throw new InvalidOperationException($"Duplicate Timeline Track Stable ID: {track._stableTrackId}");
            }
        }

        private static void ValidateRuntimeClipIdentities(
            FanlightTimelineTrackAsset track,
            HashSet<string> usedClipIds)
        {
            foreach (var clip in track.GetClips())
            {
                if (clip.asset is not FanlightTimelineClipAsset asset)
                {
                    throw new InvalidOperationException("Typed Timeline tracks require typed clips.");
                }

                if (string.IsNullOrWhiteSpace(asset.StableClipId))
                {
                    throw new InvalidOperationException("Timeline Clip Stable ID must be assigned during authoring.");
                }

                if (!usedClipIds.Add(asset.StableClipId))
                {
                    throw new InvalidOperationException($"Duplicate Timeline Clip Stable ID: {asset.StableClipId}");
                }
            }
        }


#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            EnsureAuthoringTrackIdentity();
            EnsureAuthoringClipIdentities();
        }

        private void EnsureAuthoringClipIdentities()
        {
            var usedClipIds = new HashSet<string>(StringComparer.Ordinal);

            if (timelineAsset == null)
            {
                EnsureAuthoringClipIdentities(this, usedClipIds);
                return;
            }

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is FanlightTimelineTrackAsset typed)
                {
                    EnsureAuthoringClipIdentities(typed, usedClipIds);
                }
            }
        }

        private static void EnsureAuthoringClipIdentities(
            FanlightTimelineTrackAsset track,
            HashSet<string> usedClipIds)
        {
            foreach (var clip in track.GetClips())
            {
                if (clip.asset is FanlightTimelineClipAsset asset)
                {
                    asset.EnsureAuthoringIdentity(usedClipIds);
                }
            }
        }

        private void EnsureAuthoringTrackIdentity()
        {
            if (string.IsNullOrWhiteSpace(_stableTrackId) || HasEarlierDuplicate())
            {
                _stableTrackId = Guid.NewGuid().ToString("N");
            }
        }

        private bool HasEarlierDuplicate()
        {
            if (timelineAsset == null) return false;

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track == this) return false;

                if (track is FanlightTimelineTrackAsset typed && string.Equals(typed._stableTrackId, _stableTrackId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
#endif
    }
}
