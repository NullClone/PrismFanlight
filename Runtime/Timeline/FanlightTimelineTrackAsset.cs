using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineTrackAsset : TrackAsset
    {
        [SerializeField, HideInInspector] private string _stableTrackId = string.Empty;

        internal abstract FanlightTimelinePatchKind PatchKind { get; }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            EnsureTrackIdentity();
            EnsureClipIdentities();
            var ranges = new FanlightTimelineClipRange[inputCount];
            var starts = new double[inputCount];
            var ends = new double[inputCount];
            var stableClipIds = new string[inputCount];
            var index = 0;
            foreach (var clip in GetClips())
            {
                if (index >= inputCount) break;
                if (clip.asset is not FanlightTimelineClipAsset asset)
                    throw new InvalidOperationException("Typed Timeline tracks require typed clips.");
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
            mixer.GetBehaviour().Configure($"timeline:{_stableTrackId}", PatchKind, ranges);
            return mixer;
        }

        protected virtual void OnValidate()
        {
            EnsureTrackIdentity();
            EnsureClipIdentities();
        }

        private void EnsureClipIdentities()
        {
            var usedClipIds = new HashSet<string>(StringComparer.Ordinal);
            if (timelineAsset == null)
            {
                EnsureClipIdentities(this, usedClipIds);
                return;
            }

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is FanlightTimelineTrackAsset typed) EnsureClipIdentities(typed, usedClipIds);
            }
        }

        private static void EnsureClipIdentities(
            FanlightTimelineTrackAsset track,
            HashSet<string> usedClipIds)
        {
            foreach (var clip in track.GetClips())
            {
                if (clip.asset is FanlightTimelineClipAsset asset) asset.EnsureIdentity(usedClipIds);
            }
        }

        private void EnsureTrackIdentity()
        {
            if (string.IsNullOrWhiteSpace(_stableTrackId) || HasEarlierDuplicate())
                _stableTrackId = Guid.NewGuid().ToString("N");
        }

        private bool HasEarlierDuplicate()
        {
            if (timelineAsset == null) return false;
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track == this) return false;
                if (track is FanlightTimelineTrackAsset typed
                    && string.Equals(typed._stableTrackId, _stableTrackId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
