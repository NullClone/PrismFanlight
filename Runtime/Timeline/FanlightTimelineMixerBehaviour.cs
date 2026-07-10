using System;
using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight
{
    public sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        private PrismFanlight _lastTarget;
        private bool _hasActiveCue;


        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fanlight = playerData as PrismFanlight;

            if (_lastTarget != fanlight)
            {
                if (_lastTarget != null)
                {
                    _lastTarget.ClearResolvedStateOverride();
                }

                _hasActiveCue = false;
            }

            _lastTarget = fanlight;
            if (fanlight == null) return;

            var time = (float)playable.GetTime();
            var isTimeJump = IsTimeJump(playable, info);
            var color = Color.clear;
            var intensity = 0.0f;
            var totalWeight = 0.0f;

            for (var i = 0; i < playable.GetInputCount(); i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= 0.0f) continue;

                var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)playable.GetInput(i);
                var behaviour = input.GetBehaviour();

                color += behaviour.Color * weight;
                intensity += behaviour.Intensity * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.0f)
            {
                fanlight.ClearResolvedStateOverride();
                _hasActiveCue = false;

                return;
            }

            var context = FanlightEvaluationContext.Timeline(time, !_hasActiveCue || isTimeJump);
            var baseState = fanlight.ResolveState(context);
            var state = FanlightStateComposer.ApplyColor(
                baseState,
                color,
                intensity,
                totalWeight);

            fanlight.SetResolvedStateOverride(state);
            _hasActiveCue = true;
        }

        private static bool IsTimeJump(Playable playable, FrameData info)
        {
            if (info.seekOccurred
                || info.timeLooped
                || info.evaluationType == FrameData.EvaluationType.Evaluate)
            {
                return true;
            }

            var actualDelta = playable.GetTime() - playable.GetPreviousTime();
            var expectedDelta = info.deltaTime * info.effectiveSpeed;

            return Math.Abs(actualDelta - expectedDelta) > 0.000001;
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearResolvedStateOverride();
                _lastTarget = null;
            }

            _hasActiveCue = false;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearResolvedStateOverride();
                _lastTarget = null;
            }

            _hasActiveCue = false;
        }
    }
}
