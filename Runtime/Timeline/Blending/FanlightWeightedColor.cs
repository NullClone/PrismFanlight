using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal struct FanlightWeightedColor
    {
        private Color _sum;
        private double _weight;


        internal void Add(Color value, float weight)
        {
            if (!FanlightStateValidation.IsFinite(value.r)
                || !FanlightStateValidation.IsFinite(value.g)
                || !FanlightStateValidation.IsFinite(value.b)
                || !FanlightStateValidation.IsFinite(value.a))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (FanlightStateValidation.IsFinite(weight) && weight > 0f)
            {
                var linear = QualitySettings.activeColorSpace == ColorSpace.Linear ? value : value.linear;
                _sum += linear * weight;
                _weight += weight;
            }
        }

        internal Color Value(Color fallback)
        {
            if (_weight <= 0d) return fallback;

            var linear = _sum * (float)(1d / _weight);

            return QualitySettings.activeColorSpace == ColorSpace.Linear ? linear : linear.gamma;
        }
    }
}
