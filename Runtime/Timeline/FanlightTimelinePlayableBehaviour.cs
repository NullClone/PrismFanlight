using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight
{
    public sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        public PrismFanlight Target;
        public FanlightTempoSettings Tempo;
        public FanlightMotionPreset MotionPreset;
        public FanlightColorPreset ColorPreset;
        public FanlightAudienceSettings Audience;
        public FanlightLodSettings Lod;
        public FanlightRandomSettings Random;
        private PrismFanlight _lastTarget;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fanlight = Target != null ? Target : playerData as PrismFanlight;
            if (fanlight == null) return;

            _lastTarget = fanlight;
            var time = (float)playable.GetTime();
            var tempo = Tempo;
            tempo.clockSource = FanlightTempoClockSource.ManualTime;
            tempo.manualTime = Mathf.Max(0.0f, time);

            var motion = MotionPreset != null ? MotionPreset.Settings : fanlight.GetMotion();
            var color = ColorPreset != null ? ColorPreset.Settings : fanlight.GetColorSettings();

            fanlight.SetResolvedStateOverride(new FanlightResolvedState(
                tempo.Evaluate(time),
                motion,
                color,
                Audience,
                Lod,
                Random,
                fanlight.SwingTarget != null ? fanlight.SwingTarget.position : Vector3.zero,
                fanlight.transform.localToWorldMatrix,
                time,
                time));
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            var fanlight = Target != null ? Target : _lastTarget;
            if (fanlight != null)
            {
                fanlight.ClearResolvedStateOverride();
            }
        }
    }
}
