using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightPaletteGradientPlayableAsset))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(1.0f, 0.35f, 0.65f)]
    public sealed class FanlightPaletteGradientTrack : TrackAsset
    {
        [SerializeField, HideInInspector] private string _stableId = string.Empty;
        [SerializeField] private int _priority;
        [SerializeField, HideInInspector] private bool _priorityInitialized;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            EnsureIdentity();
            var mixer = ScriptPlayable<FanlightPaletteGradientMixerBehaviour>.Create(graph, inputCount);
            mixer.GetBehaviour().Configure($"timeline:{_stableId}", _priority);
            return mixer;
        }

        private void OnValidate() => EnsureIdentity();

        private void EnsureIdentity()
        {
            if (string.IsNullOrEmpty(_stableId)) _stableId = Guid.NewGuid().ToString("N");
            if (HasEarlierDuplicate()) _stableId = Guid.NewGuid().ToString("N");
            if (_priorityInitialized) return;
            _priority = GetTrackSortOrder();
            _priorityInitialized = true;
        }

        private bool HasEarlierDuplicate()
        {
            if (timelineAsset == null) return false;
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track == this) return false;
                if (track is FanlightPaletteGradientTrack other
                    && string.Equals(other._stableId, _stableId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private int GetTrackSortOrder()
        {
            if (timelineAsset == null) return 0;

            var order = 0;
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track == this) return order;
                order++;
            }

            return order;
        }
    }
}
