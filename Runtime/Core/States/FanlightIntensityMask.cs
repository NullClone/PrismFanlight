using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntensityMask
    {
        // Fields

        [SerializeField]
        private FanlightIntensityMaskMode _mode;

        [SerializeField]
        private Vector2 _origin;

        [SerializeField]
        private Vector2 _direction;

        [SerializeField]
        private float _width;

        [SerializeField]
        private float _progress;

        [SerializeField]
        private float _radius;

        [SerializeField]
        private float _softness;

        [SerializeField]
        private bool _invert;


        // Properties

        internal FanlightIntensityMaskMode Mode => _mode;

        internal Vector2 Origin => _origin;

        internal Vector2 Direction => _direction;

        internal float Width => _width;

        internal float Progress => _progress;

        internal float Radius => _radius;

        internal float Softness => _softness;

        internal bool Invert => _invert;


        // Methods

        internal FanlightIntensityMask(
            FanlightIntensityMaskMode mode,
            Vector2 origin,
            Vector2 direction,
            float width,
            float progress,
            float radius,
            float softness,
            bool invert)
        {
            _mode = mode;
            _origin = origin;
            _direction = direction;
            _width = width;
            _progress = progress;
            _radius = radius;
            _softness = softness;
            _invert = invert;
            ValidateAndNormalize();
        }

        internal FanlightIntensityMask Validated()
        {
            var value = this;
            value.ValidateAndNormalize();
            return value;
        }

        internal bool ContentEquals(in FanlightIntensityMask other)
        {
            if (_mode != other._mode) return false;

            return _mode switch
            {
                FanlightIntensityMaskMode.None => true,
                FanlightIntensityMaskMode.LinearWipe => _origin.Equals(other._origin)
                                                        && _direction.Equals(other._direction)
                                                        && _width.Equals(other._width)
                                                        && _progress.Equals(other._progress)
                                                        && _softness.Equals(other._softness)
                                                        && _invert == other._invert,
                FanlightIntensityMaskMode.RadialWipe => _origin.Equals(other._origin)
                                                        && _radius.Equals(other._radius)
                                                        && _softness.Equals(other._softness)
                                                        && _invert == other._invert,
                _ => false
            };
        }

        private void ValidateAndNormalize()
        {
            switch (_mode)
            {
                case FanlightIntensityMaskMode.None:
                    break;
                case FanlightIntensityMaskMode.LinearWipe:
                    _origin = FanlightStateValidation.RequireFinite(_origin, nameof(_origin));
                    _direction = FanlightStateValidation.RequireDirection(_direction, nameof(_direction));
                    _width = FanlightStateValidation.RequireMinimumExclusive(_width, 0f, nameof(_width));
                    _progress = FanlightStateValidation.RequireRange(_progress, 0f, 1f, nameof(_progress));
                    _softness = FanlightStateValidation.RequireMinimum(_softness, 0f, nameof(_softness));
                    break;
                case FanlightIntensityMaskMode.RadialWipe:
                    _origin = FanlightStateValidation.RequireFinite(_origin, nameof(_origin));
                    _radius = FanlightStateValidation.RequireMinimum(_radius, 0f, nameof(_radius));
                    _softness = FanlightStateValidation.RequireMinimum(_softness, 0f, nameof(_softness));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode));
            }
        }
    }
}
