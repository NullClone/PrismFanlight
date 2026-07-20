using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineClipAsset : PlayableAsset, ITimelineClipAsset
    {
        // Fields

        [SerializeField, HideInInspector]
        private string _stableClipId = string.Empty;

        [SerializeField]
        private AnimationCurve _localWeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [SerializeField]
        private FanlightTimelineHoldMode _holdMode;


        // Properties

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        internal string StableClipId => _stableClipId;

        internal AnimationCurve LocalWeightCurve => _localWeightCurve;

        internal FanlightTimelineHoldMode HoldMode => _holdMode;

        internal abstract FanlightTimelineClipValue Value { get; }


        // Methods

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            if (string.IsNullOrWhiteSpace(_stableClipId))
            {
                throw new InvalidOperationException("Timeline Clip Stable ID must be assigned during authoring.");
            }

            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            playable.GetBehaviour().Configure(_stableClipId, Value, _localWeightCurve, _holdMode);
            return playable;
        }


#if UNITY_EDITOR
        internal void EnsureAuthoringIdentity(HashSet<string> usedIds)
        {
            if (string.IsNullOrWhiteSpace(_stableClipId) || usedIds != null && !usedIds.Add(_stableClipId))
            {
                do
                {
                    _stableClipId = Guid.NewGuid().ToString("N");
                } while (usedIds != null && !usedIds.Add(_stableClipId));
            }
        }

        protected virtual void OnValidate()
        {
            EnsureAuthoringIdentity(null);

            if (_holdMode is not FanlightTimelineHoldMode.None and not FanlightTimelineHoldMode.HoldLast)
            {
                _holdMode = FanlightTimelineHoldMode.None;
            }

            _localWeightCurve ??= AnimationCurve.Linear(0f, 1f, 1f, 1f);
        }
#endif
    }
}
