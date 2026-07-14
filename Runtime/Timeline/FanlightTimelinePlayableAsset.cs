using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelinePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        // Fields

        [HideInInspector]
        public bool _overrideColor;

        [HideInInspector]
        public bool _overrideMotion;

        [HideInInspector]
        public bool _overrideTempo;

        [HideInInspector]
        public bool _overrideAudience;

        [HideInInspector]
        public FanlightColorSettings _colorSettings = FanlightColorSettings.Default();
        public FanlightMotionSettings _motionSettings = FanlightMotionSettings.Default();
        public FanlightTempoSettings _tempoSettings = FanlightTempoSettings.Default();
        public FanlightAudienceSettings _audienceSettings = FanlightAudienceSettings.Default();

        [SerializeField]
        private FanlightTimelineOverrideSelection _overrides = new();

        [SerializeField, HideInInspector]
        private bool _usesPathOverrides;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public System.Collections.Generic.IReadOnlyList<string> OverridePaths => GetOverrides().Paths;


        // Methods

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Asset = this;
            behaviour.Color = _colorSettings.Validated();
            behaviour.Motion = _motionSettings.Validated();
            behaviour.Tempo = _tempoSettings.Validated();
            behaviour.Audience = _audienceSettings.Validated();
            behaviour.Overrides = GetOverrides();
            return playable;
        }

        internal FanlightColorSettings GetColorSettings() => _colorSettings.Validated();

        internal FanlightMotionSettings GetMotionSettings() => _motionSettings.Validated();

        internal FanlightTempoSettings GetTempoSettings() => _tempoSettings.Validated();

        internal FanlightAudienceSettings GetAudienceSettings() => _audienceSettings.Validated();

        internal bool HasLegacyColorOverrides()
        {
            if (_overrideColor) return true;

            foreach (var path in GetOverrides().Paths)
            {
                if (path.StartsWith("color.", System.StringComparison.Ordinal)) return true;
            }

            return false;
        }

        internal void UpgradeLegacyOverrides()
        {
            EnsureOverrides();

            _ = _overrides.Paths;

            if (_usesPathOverrides) return;

            if (_overrideMotion) _overrides.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Motion), true);
            if (_overrideTempo) _overrides.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Tempo), true);
            if (_overrideAudience) _overrides.SetAll(FanlightTimelineOverrideSchema.GetPaths(FanlightTimelineSettingsGroup.Audience), true);

            _usesPathOverrides = true;
        }

        internal FanlightTimelineOverrideSelection GetTimelineOverrides() => GetOverrides();

        private FanlightTimelineOverrideSelection GetOverrides()
        {
            EnsureOverrides();

            if (_usesPathOverrides || _overrides.Paths.Count > 0) return _overrides;

            var legacy = new FanlightTimelineOverrideSelection();
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
