using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineTrackContribution
    {
        // Fields

        private const float WeightEpsilon = 0.0001f;

        private readonly Dictionary<string, FanlightTimelineParameterContribution> _parameters = new();
        private readonly List<FanlightTimelineParameterContribution> _activeParameters = new();
        private readonly List<string> _unsupportedPaths = new();
        private int _version;
        private int _mappedParameterCount;


        // Properties

        public float Time { get; private set; }

        public bool IsTimeJump { get; private set; }

        public int SortOrder { get; private set; }

        public IReadOnlyList<FanlightTimelineParameterContribution> Parameters => _activeParameters;

        public bool HasOverrides => _activeParameters.Count > 0;
        public IReadOnlyList<string> UnsupportedPaths => _unsupportedPaths;
        public bool HasMappedOverrides => _mappedParameterCount > 0;

        public FanlightShowPatch BuildPatch(FanlightShowState baseState)
        {
            _mappedParameterCount = 0;
            var builder = new FanlightShowPatchBuilder(baseState);
            for (var i = 0; i < _activeParameters.Count; i++)
            {
                var parameter = _activeParameters[i];
                if (!FanlightLegacyIntentAdapter.TryAddLegacyParameter(
                        builder,
                        parameter.Descriptor.Path,
                        parameter.Value,
                        baseState))
                {
                    _unsupportedPaths.Add(parameter.Descriptor.Path);
                }
                else
                {
                    _mappedParameterCount++;
                }
            }

            return builder.Build();
        }


        // Methods

        public void Begin(float time, bool isTimeJump, int sortOrder)
        {
            _version++;
            Time = time;
            IsTimeJump = isTimeJump;
            SortOrder = sortOrder;
            _activeParameters.Clear();
            _unsupportedPaths.Clear();
        }

        public void Add(FanlightTimelinePlayableBehaviour behaviour, float weight)
        {
            if (weight <= WeightEpsilon) return;

            var overrides = behaviour.GetOverrides();
            if (overrides == null) return;

            foreach (var path in overrides.Paths)
            {
                if (!FanlightTimelineOverrideSchema.TryGet(path, out var descriptor)) continue;
                if (descriptor.Group == FanlightTimelineSettingsGroup.Color) continue;

                Add(descriptor, GetRootValue(behaviour, descriptor.Group), weight);
            }

            if (behaviour.HasLegacyColorOverrides())
            {
                var color = behaviour.GetColor();
                foreach (var descriptor in FanlightTimelineOverrideSchema.GetGroup(FanlightTimelineSettingsGroup.Color))
                {
                    Add(descriptor, color, weight);
                }
            }
        }

        public void AddPalette(FanlightPaletteGradientPlayableBehaviour behaviour, float normalizedTime, float weight)
        {
            if (behaviour?.Asset == null || weight <= WeightEpsilon) return;

            var color = behaviour.Evaluate(normalizedTime);
            foreach (var path in behaviour.Asset.GetOverridePaths())
            {
                if (!FanlightTimelineOverrideSchema.TryGet(path, out var descriptor)) continue;
                Add(descriptor, color, weight);
            }
        }

        private void Add(FanlightTimelineOverrideDescriptor descriptor, object root, float weight)
        {
            if (!_parameters.TryGetValue(descriptor.Path, out var parameter))
            {
                parameter = new FanlightTimelineParameterContribution(descriptor);
                _parameters.Add(descriptor.Path, parameter);
            }

            if (parameter.Version != _version)
            {
                parameter.Reset(_version);
                _activeParameters.Add(parameter);
            }

            parameter.Add(root, weight);
        }

        private static object GetRootValue(FanlightTimelinePlayableBehaviour behaviour, FanlightTimelineSettingsGroup group)
        {
            return group switch
            {
                FanlightTimelineSettingsGroup.Color => behaviour.GetColor(),
                FanlightTimelineSettingsGroup.Motion => behaviour.GetMotion(),
                FanlightTimelineSettingsGroup.Tempo => behaviour.GetTempo(),
                FanlightTimelineSettingsGroup.Audience => behaviour.GetAudience(),
                _ => null
            };
        }
    }
}
