using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelinePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        // Fields

        [HideInInspector]
        [ColorUsage(false, true)]
        public Color _color = Color.white;

        [HideInInspector]
        [Min(0.0f)]
        public float _intensity = 20.0f;

        public bool _overrideColor;

        public FanlightColorSettings _colorSettings = FanlightColorSettings.Default();

        public bool _overrideMotion;

        public FanlightMotionSettings _motion = FanlightMotionSettings.Default();

        public bool _overrideTempo;

        public FanlightTempoSettings _tempo = FanlightTempoSettings.Default();

        public bool _overrideAudience;

        public FanlightAudienceSettings _audience = FanlightAudienceSettings.Default();


        // Properties

        public ClipCaps clipCaps => ClipCaps.Blending;


        // Methods

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);

            var behaviour = playable.GetBehaviour();
            behaviour.OverrideColor = _overrideColor;
            behaviour.Color = _colorSettings.Validated();
            behaviour.OverrideMotion = _overrideMotion;
            behaviour.Motion = _motion.Validated();
            behaviour.OverrideTempo = _overrideTempo;
            behaviour.Tempo = _tempo.Validated();
            behaviour.OverrideAudience = _overrideAudience;
            behaviour.Audience = _audience.Validated();

            return playable;
        }
    }
}
