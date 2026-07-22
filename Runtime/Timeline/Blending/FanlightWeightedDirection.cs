using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal struct FanlightWeightedDirection
    {
        // Fields

        private Vector3 _first;
        private Vector3 _second;
        private double _firstWeight;
        private double _secondWeight;
        private int _count;


        // Methods

        internal void Add(Vector3 value, float weight)
        {
            if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;

            value = FanlightStateValidation.RequireDirection(value, nameof(value));

            if (_count == 0)
            {
                _first = value;
                _firstWeight = weight;
                _count = 1;
                return;
            }

            if (_count == 1)
            {
                _second = value;
                _secondWeight = weight;
                _count = 2;
                return;
            }

            throw new InvalidOperationException("Timeline Direction blending supports at most two active clips.");
        }

        internal Vector3 Value(Vector3 fallback)
        {
            if (_count == 0) return FanlightStateValidation.RequireDirection(fallback, nameof(fallback));
            if (_count == 1) return _first;

            var weight = (float)(_secondWeight / (_firstWeight + _secondWeight));
            return FanlightDirectionInterpolation.Interpolate(_first, _second, weight);
        }
    }
}
