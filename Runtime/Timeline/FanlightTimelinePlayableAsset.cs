using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight
{
    public sealed class FanlightTimelinePlayableAsset : PlayableAsset, ITimelineClipAsset, ISerializationCallbackReceiver
    {
        private const int CurrentSerializedVersion = 1;

        // Retained for Timeline clips authored before Color became optional and
        // gained Random/Gradient support.
        [HideInInspector]
        [ColorUsage(false, true)]
        public Color color = Color.white;

        [HideInInspector]
        [Min(0.0f)]
        public float intensity = 20.0f;

        [Header("Optional Overrides")]
        [Tooltip("Blend this color settings block over the bound Prism Fanlight settings.")]
        public bool overrideColor = true;

        public FanlightColorSettings colorSettings = FanlightColorSettings.Default();

        [Tooltip("Blend this motion settings block over the bound Prism Fanlight settings.")]
        public bool overrideMotion;

        public FanlightMotionSettings motion = FanlightMotionSettings.Default();

        [Tooltip("Blend BPM, meter, and timing offsets over the bound Prism Fanlight settings. Timeline time always remains the song-time source.")]
        public bool overrideTempo;

        public FanlightTempoSettings tempo = FanlightTempoSettings.Default();

        [Tooltip("Blend this audience settings block over the bound Prism Fanlight settings.")]
        public bool overrideAudience;

        public FanlightAudienceSettings audience = FanlightAudienceSettings.Default();

        [SerializeField]
        private int _serializedVersion = CurrentSerializedVersion;

        // Methods

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);

            var behaviour = playable.GetBehaviour();
            behaviour.OverrideColor = overrideColor;
            behaviour.Color = colorSettings.Validated();
            behaviour.OverrideMotion = overrideMotion;
            behaviour.Motion = motion.Validated();
            behaviour.OverrideTempo = overrideTempo;
            behaviour.Tempo = tempo.Validated();
            behaviour.OverrideAudience = overrideAudience;
            behaviour.Audience = audience.Validated();

            return playable;
        }

        public void OnBeforeSerialize()
        {
            _serializedVersion = CurrentSerializedVersion;
        }

        public void OnAfterDeserialize()
        {
            if (_serializedVersion >= CurrentSerializedVersion) return;

            colorSettings = FanlightColorSettings.Default();
            colorSettings.primaryColor = color;
            colorSettings.intensity = Mathf.Max(0.0f, intensity);
            overrideColor = true;
            _serializedVersion = CurrentSerializedVersion;
        }
    }
}
