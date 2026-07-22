using System;
using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal struct FanlightDiscreteValue<T>
    {
        private bool _hasValue;
        private T _value;
        private float _weight;
        private double _startSeconds;


        internal void Consider(T value, float weight, double startSeconds)
        {
            if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;

            if (_hasValue && startSeconds == _startSeconds && !EqualityComparer<T>.Default.Equals(value, _value))
            {
                throw new InvalidOperationException("Different discrete Timeline values share the same clip start time.");
            }

            if ((_hasValue && weight < _weight) || (_hasValue && weight == _weight && startSeconds <= _startSeconds)) return;

            _hasValue = true;
            _value = value;
            _weight = weight;
            _startSeconds = startSeconds;
        }

        internal T Value(T fallback) => _hasValue ? _value : fallback;
    }
}
