using System;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineParameterContribution
    {
        // Fields

        private float _strongestWeight;


        // Properties

        public FanlightTimelineOverrideDescriptor Descriptor { get; }

        public int Version { get; private set; }

        public object Value { get; private set; }

        public float Weight { get; private set; }


        // Methods

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

            _strongestWeight = Math.Max(_strongestWeight, weight);
            Weight += weight;
        }
    }
}
