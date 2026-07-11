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


        // Methods

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
            var color = default(FanlightColorSettings);
            var colorWeight = 0.0f;
            var colorDiscrete = default(FanlightColorSettings);
            var colorDiscreteWeight = 0.0f;
            var motion = default(FanlightMotionSettings);
            var motionWeight = 0.0f;
            var motionDiscrete = default(FanlightMotionSettings);
            var motionDiscreteWeight = 0.0f;
            var tempo = default(FanlightTempoSettings);
            var tempoWeight = 0.0f;
            var tempoDiscrete = default(FanlightTempoSettings);
            var tempoDiscreteWeight = 0.0f;
            var audience = default(FanlightAudienceSettings);
            var audienceWeight = 0.0f;
            var audienceDiscrete = default(FanlightAudienceSettings);
            var audienceDiscreteWeight = 0.0f;

            for (var i = 0; i < playable.GetInputCount(); i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= WeightEpsilon) continue;

                var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)playable.GetInput(i);
                var behaviour = input.GetBehaviour();

                if (behaviour.OverrideColor)
                {
                    if (weight > colorDiscreteWeight)
                    {
                        colorDiscrete = behaviour.Color;
                        colorDiscreteWeight = weight;
                    }

                    color = colorWeight <= WeightEpsilon
                        ? behaviour.Color
                        : FanlightStateComposer.BlendColorSettings(color, behaviour.Color, weight / (colorWeight + weight));
                    colorWeight += weight;
                }

                if (behaviour.OverrideMotion)
                {
                    if (weight > motionDiscreteWeight)
                    {
                        motionDiscrete = behaviour.Motion;
                        motionDiscreteWeight = weight;
                    }

                    motion = motionWeight <= WeightEpsilon
                        ? behaviour.Motion
                        : FanlightStateComposer.BlendMotion(motion, behaviour.Motion, weight / (motionWeight + weight));
                    motionWeight += weight;
                }

                if (behaviour.OverrideTempo)
                {
                    if (weight > tempoDiscreteWeight)
                    {
                        tempoDiscrete = behaviour.Tempo;
                        tempoDiscreteWeight = weight;
                    }

                    tempo = tempoWeight <= WeightEpsilon
                        ? behaviour.Tempo
                        : FanlightStateComposer.BlendTempoSettings(tempo, behaviour.Tempo, weight / (tempoWeight + weight));
                    tempoWeight += weight;
                }

                if (behaviour.OverrideAudience)
                {
                    if (weight > audienceDiscreteWeight)
                    {
                        audienceDiscrete = behaviour.Audience;
                        audienceDiscreteWeight = weight;
                    }

                    audience = audienceWeight <= WeightEpsilon
                        ? behaviour.Audience
                        : FanlightStateComposer.BlendAudience(audience, behaviour.Audience, weight / (audienceWeight + weight));
                    audienceWeight += weight;
                }
            }

            if (colorWeight > WeightEpsilon)
            {
                color.mode = colorDiscrete.mode;
                color.paletteColors = colorDiscrete.paletteColors;
            }

            if (motionWeight > WeightEpsilon)
            {
                motion.direction.swingMode = motionDiscrete.direction.swingMode;
                motion.noise.noiseOctaves = motionDiscrete.noise.noiseOctaves;
            }

            if (tempoWeight > WeightEpsilon)
            {
                tempo.beatsPerBar = tempoDiscrete.beatsPerBar;
            }

            if (audienceWeight > WeightEpsilon)
            {
                audience.enabled = audienceDiscrete.enabled;
                audience.handZone.zone = audienceDiscrete.handZone.zone;
            }

            if (colorWeight <= WeightEpsilon
                && motionWeight <= WeightEpsilon
                && tempoWeight <= WeightEpsilon
                && audienceWeight <= WeightEpsilon)
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
                colorWeight);

            state = FanlightStateComposer.ApplyMotion(state, motion, motionWeight);
            state = FanlightStateComposer.ApplyTempo(state, tempo, tempoWeight, time);
            state = FanlightStateComposer.ApplyAudience(state, audience, audienceWeight);

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
