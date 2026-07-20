using System;
using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal struct FanlightWeightedFloat
    {
        private double _sum;
        private double _weight;


        internal void Add(float value, float weight)
        {
            if (!FanlightStateValidation.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;

            _sum += value * weight;
            _weight += weight;
        }

        internal float Value(float fallback) => _weight > 0d ? (float)(_sum / _weight) : fallback;
    }
}
