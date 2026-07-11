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

            foreach (var path in behaviour.Overrides.Paths)
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
                FanlightTimelineSettingsGroup.Color => behaviour.Color,
                FanlightTimelineSettingsGroup.Motion => behaviour.Motion,
                FanlightTimelineSettingsGroup.Tempo => behaviour.Tempo,
                FanlightTimelineSettingsGroup.Audience => behaviour.Audience,
                _ => null
            };
        }
    }

    internal sealed class FanlightTimelineParameterContribution
    {
        public FanlightTimelineOverrideDescriptor Descriptor { get; }
        public int Version { get; private set; }
        public object Value { get; private set; }
        public float Weight { get; private set; }

        private float _strongestWeight;

        public FanlightTimelineParameterContribution(FanlightTimelineOverrideDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public void Reset(int version)
        {
            Version = version;
            Value = null;
            Weight = 0.0f;
            _strongestWeight = 0.0f;
        }

        public void Add(object root, float weight)
        {
            var incoming = Descriptor.GetValue(root);
            if (Weight <= 0.0f)
            {
                Value = incoming;
            }
            else if (Descriptor.IsDiscrete())
            {
                if (weight >= _strongestWeight) Value = incoming;
            }
            else
            {
                Value = Descriptor.Blend(Value, incoming, weight / (Weight + weight));
            }

            _strongestWeight = System.Math.Max(_strongestWeight, weight);
            Weight += weight;
        }
    }
}
