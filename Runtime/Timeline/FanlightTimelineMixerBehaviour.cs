using System;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private const float WeightEpsilon = 0.0001f;

        private PrismFanlight _lastTarget;
        private bool _hasActiveCue;
        private int _sortOrder;

        private readonly FanlightTimelineTrackContribution _contribution = new();


        // Methods

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fanlight = playerData as PrismFanlight;

            if (_lastTarget != fanlight)
            {
                if (_lastTarget != null)
                {
                    _lastTarget.ClearTimelineContribution(this);
                }

                _hasActiveCue = false;
            }

            _lastTarget = fanlight;

            if (fanlight == null) return;

            var time = (float)playable.GetTime();
            var isTimeJump = IsTimeJump(playable, info);

            _contribution.Begin(time, !_hasActiveCue || isTimeJump, _sortOrder);

            for (var i = 0; i < playable.GetInputCount(); i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= WeightEpsilon) continue;

                var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)playable.GetInput(i);
                _contribution.Add(input.GetBehaviour(), weight);
            }

            if (!_contribution.HasOverrides)
            {
                fanlight.ClearTimelineContribution(this);
                _hasActiveCue = false;
                return;
            }

            fanlight.SetTimelineContribution(this, _contribution);
            _hasActiveCue = true;
        }

        public void Configure(int sortOrder)
        {
            _sortOrder = sortOrder;
        }

        private static bool IsTimeJump(Playable playable, FrameData info)
        {
            if (info.seekOccurred || info.timeLooped || info.evaluationType == FrameData.EvaluationType.Evaluate) return true;

            var actualDelta = playable.GetTime() - playable.GetPreviousTime();
            var expectedDelta = info.deltaTime * info.effectiveSpeed;

            return Math.Abs(actualDelta - expectedDelta) > 0.000001;
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearTimelineContribution(this);
            }

            _lastTarget = null;
            _hasActiveCue = false;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearTimelineContribution(this);
            }

            _lastTarget = null;
            _hasActiveCue = false;
        }
    }
}
