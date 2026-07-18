using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineClipAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField, HideInInspector] private string _stableClipId = string.Empty;
        [SerializeField] private AnimationCurve _localWeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private int _priority;
        [SerializeField] private FanlightTimelineHoldMode _holdMode;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        internal string StableClipId => _stableClipId;
        internal AnimationCurve LocalWeightCurve => _localWeightCurve;
        internal int Priority => _priority;
        internal FanlightTimelineHoldMode HoldMode => _holdMode;
        internal abstract FanlightShowPatch Patch { get; }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            EnsureIdentity(null);
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            playable.GetBehaviour().Configure(
                _stableClipId,
                Patch,
                _localWeightCurve,
                _priority,
                _holdMode);
            return playable;
        }

        internal void EnsureIdentity(HashSet<string> usedIds)
        {
            if (_holdMode is not FanlightTimelineHoldMode.None and not FanlightTimelineHoldMode.HoldLast)
                _holdMode = FanlightTimelineHoldMode.None;
            _localWeightCurve ??= AnimationCurve.Linear(0f, 1f, 1f, 1f);
            if (string.IsNullOrWhiteSpace(_stableClipId) || usedIds != null && !usedIds.Add(_stableClipId))
            {
                do
                {
                    _stableClipId = Guid.NewGuid().ToString("N");
                }
                while (usedIds != null && !usedIds.Add(_stableClipId));
            }
        }

        protected virtual void OnValidate() => EnsureIdentity(null);
    }
}
