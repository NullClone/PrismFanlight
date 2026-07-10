using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight
{
    public sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        private PrismFanlight _lastTarget;


        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fanlight = playerData as PrismFanlight;

            if (fanlight == null) return;
            _lastTarget = fanlight;

            var time = (float)playable.GetTime();
            var tempo = fanlight.Tempo;
            tempo.clockSource = FanlightTempoClockSource.ManualTime;
            tempo.manualTime = Mathf.Max(0.0f, time);
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

                return;
            }

            var blendWeight = Mathf.Clamp01(totalWeight);
            var colorSettings = fanlight.GetColorSettings();
            colorSettings.mode = FanlightColorMode.Single;
            colorSettings.primaryColor = Color.Lerp(colorSettings.primaryColor, color * (1.0f / totalWeight), blendWeight);
            colorSettings.intensity = Mathf.Lerp(colorSettings.intensity, intensity / totalWeight, blendWeight);

            var state = new FanlightResolvedState(
                tempo.Evaluate(time),
                fanlight.GetMotionSettings(),
                colorSettings,
                fanlight.GetAudienceSettings(),
                fanlight.GetLodSettings(),
                fanlight.GetRandomSettings(),
                fanlight.SwingTarget != null ? fanlight.SwingTarget.position : Vector3.zero,
                fanlight.transform.localToWorldMatrix,
                time,
                time);

            fanlight.SetResolvedStateOverride(state);
            fanlight.Render(state);
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearResolvedStateOverride();
            }
        }
    }
}
