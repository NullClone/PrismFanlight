using PrismFanlight.Time;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTempoMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private PrismFanlight _lastTarget;


        // Properties

        internal FanlightTempoRuntimeDefinition Definition { get; private set; }


        // Methods

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as PrismFanlight;

            if (_lastTarget != target)
            {
                ClearCandidate();
                _lastTarget = target;
            }

            if (target == null || Definition == null) return;

            target.SetScheduledTempoCandidate(
                this,
                new FanlightTempoCandidate(playable.GetTime(), Definition));
        }

        public override void OnBehaviourPause(Playable playable, FrameData info) => ClearCandidate();

        public override void OnGraphStop(Playable playable) => ClearCandidate();

        public override void OnPlayableDestroy(Playable playable) => ClearCandidate();

        internal void Configure(FanlightTempoRuntimeDefinition definition)
        {
            Definition = definition;
        }

        private void ClearCandidate()
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledTempoCandidate(this);
            }

            _lastTarget = null;
        }
    }
}
