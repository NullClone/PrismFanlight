using System;
using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal struct FanlightDiscreteValue<T>
    {
        private bool _hasValue;
        private T _value;
        private float _weight;
        private string _stableClipId;


        internal void Consider(T value, float weight, string stableClipId)
        {
            if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;

            if ((_hasValue && weight < _weight) ||
                (_hasValue && weight == _weight && string.Compare(stableClipId, _stableClipId, StringComparison.Ordinal) <= 0))
                return;

            _hasValue = true;
            _value = value;
            _weight = weight;
            _stableClipId = stableClipId;
        }

        internal T Value(T fallback) => _hasValue ? _value : fallback;
    }
}
