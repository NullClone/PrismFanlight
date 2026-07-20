using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal struct FanlightWeightedAngle
    {
        private float _degrees;
        private double _weight;


        internal void AddDegrees(float value, float weight)
        {
            if (!FanlightStateValidation.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;

            _degrees = _weight <= 0d ? value : Mathf.LerpAngle(_degrees, value, (float)(weight / (_weight + weight)));
            _weight += weight;
        }

        internal void AddRadians(float value, float weight) => AddDegrees(value * Mathf.Rad2Deg, weight);

        internal float ValueDegrees(float fallback) => _weight > 0d ? Mathf.Repeat(_degrees, 360f) : fallback;

        internal float ValueRadians(float fallback) => _weight > 0d ? Mathf.Repeat(_degrees, 360f) * Mathf.Deg2Rad : fallback;
    }
}
