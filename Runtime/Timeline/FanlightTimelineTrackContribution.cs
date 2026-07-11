using System.Collections.Generic;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineTrackContribution
    {
        // Fields

        private const float WeightEpsilon = 0.0001f;

        private readonly Dictionary<string, FanlightTimelineParameterContribution> _parameters = new();
        private readonly List<FanlightTimelineParameterContribution> _activeParameters = new();
        private int _version;


        // Properties

        public float Time { get; private set; }

        public bool IsTimeJump { get; private set; }

        public int SortOrder { get; private set; }

        public IReadOnlyList<FanlightTimelineParameterContribution> Parameters => _activeParameters;

        public bool HasOverrides => _activeParameters.Count > 0;


        // Methods

        public void Begin(float time, bool isTimeJump, int sortOrder)
        {
            _version++;
            Time = time;
            IsTimeJump = isTimeJump;
            SortOrder = sortOrder;
            _activeParameters.Clear();
        }

        public void Add(FanlightTimelinePlayableBehaviour behaviour, float weight)
        {
            if (weight <= WeightEpsilon) return;

            var overrides = behaviour.GetOverrides();
            if (overrides == null) return;

            foreach (var path in overrides.Paths)
            {
                if (!FanlightTimelineOverrideSchema.TryGet(path, out var descriptor)) continue;

                if (!_parameters.TryGetValue(path, out var parameter))
                {
                    parameter = new FanlightTimelineParameterContribution(descriptor);
                    _parameters.Add(path, parameter);
                }

                if (parameter.Version != _version)
                {
                    parameter.Reset(_version);
                    _activeParameters.Add(parameter);
                }

                parameter.Add(GetRootValue(behaviour, descriptor.Group), weight);
            }
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
