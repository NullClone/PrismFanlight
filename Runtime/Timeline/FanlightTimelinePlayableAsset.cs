using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelinePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        [HideInInspector]
        public bool _overrideColor;

        [HideInInspector]
        public bool _overrideMotion;

        [HideInInspector]
        public bool _overrideTempo;

        [HideInInspector]
        public bool _overrideAudience;

        public FanlightColorSettings _colorSettings = FanlightColorSettings.Default();
        public FanlightMotionSettings _motionSettings = FanlightMotionSettings.Default();
        public FanlightTempoSettings _tempoSettings = FanlightTempoSettings.Default();
        public FanlightAudienceSettings _audienceSettings = FanlightAudienceSettings.Default();

        [SerializeField]
        private FanlightTimelineOverrideSelection _overrides = new();

        [SerializeField, HideInInspector]
        private bool _usesPathOverrides;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Color = _colorSettings.Validated();
            behaviour.Motion = _motionSettings.Validated();
            behaviour.Tempo = _tempoSettings.Validated();
            behaviour.Audience = _audienceSettings.Validated();
            behaviour.Overrides = GetOverrides();
            return playable;
        }

        internal FanlightTimelineOverrideSelection Overrides
        {
            get
            {
                EnsureOverrides();
                return _overrides;
            }
        }

        private FanlightTimelineOverrideSelection GetOverrides()
        {
            EnsureOverrides();
            if (_usesPathOverrides) return _overrides;

            var legacy = new FanlightTimelineOverrideSelection();
            if (_overrideColor) legacy.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Color), true);
            if (_overrideMotion) legacy.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Motion), true);
            if (_overrideTempo) legacy.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Tempo), true);
            if (_overrideAudience) legacy.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Audience), true);
            return legacy;
        }

        private void EnsureOverrides()
        {
            _overrides ??= new FanlightTimelineOverrideSelection();
        }
    }
}
