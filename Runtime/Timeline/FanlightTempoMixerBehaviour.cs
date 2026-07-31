using PrismFanlight.Time;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTempoMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private PrismFanlight _lastTarget;
        private PlayableDirector _director;
        private TrackAsset _track;


        // Properties

        internal FanlightTempoRuntimeDefinition Definition { get; private set; }


        // Methods

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as PrismFanlight;

            if (_lastTarget != target)
            {
                ChangeTarget(target);
            }

            if (target == null || Definition == null) return;

            target.SetScheduledTempoCandidate(
                this,
                new FanlightTempoCandidate(playable.GetTime(), Definition));
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_lastTarget != null && !IsCurrentBinding())
            {
                _lastTarget.ClearScheduledTempoCandidate(this);
                _lastTarget.ClearHeldTimelineState();
            }

            _lastTarget = null;
        }

        internal void Configure(
            FanlightTempoRuntimeDefinition definition,
            PlayableDirector director,
            TrackAsset track)
        {
            Definition = definition;
            _director = director;
            _track = track;
        }

        private void ChangeTarget(PrismFanlight target)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledTempoCandidate(this);
                _lastTarget.ClearHeldTimelineState();
            }

            _lastTarget = target;
        }

        private bool IsCurrentBinding()
        {
            if (_director == null || _track == null || _lastTarget == null) return false;

            var binding = _director.GetGenericBinding(_track);
            if (binding == _lastTarget) return true;
            return binding is GameObject gameObject && gameObject.GetComponent<PrismFanlight>() == _lastTarget;
        }
    }
}
