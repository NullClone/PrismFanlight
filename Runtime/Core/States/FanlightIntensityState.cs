using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntensityState
    {
        // Fields

        [SerializeField]
        private float _baseIntensity;

        [SerializeField]
        private float _randomIntensity;

        [SerializeField]
        private FanlightIntensityMask _spatialMask;

        [NonSerialized]
        private FanlightIntensityMask _secondarySpatialMask;

        [NonSerialized]
        private FanlightIntensityMask _tertiarySpatialMask;

        [NonSerialized]
        private Vector3 _maskWeights;


        // Properties

        internal float BaseIntensity => _baseIntensity;

        internal float RandomIntensity => _randomIntensity;

        internal FanlightIntensityMask SpatialMask => _spatialMask;


        // Methods

        internal FanlightIntensityState(
            float baseIntensity,
            float randomIntensity,
            FanlightIntensityMask spatialMask)
        {
            _baseIntensity = FanlightStateValidation.RequireMinimum(baseIntensity, 0f, nameof(baseIntensity));
            _randomIntensity = FanlightStateValidation.RequireRange(randomIntensity, 0f, 1f, nameof(randomIntensity));
            _spatialMask = spatialMask.Validated();
            _secondarySpatialMask = default;
            _tertiarySpatialMask = default;
            _maskWeights = new Vector3(1f, 0f, 0f);
        }

        internal static FanlightIntensityState BlendMasks(
            float baseIntensity,
            float randomIntensity,
            FanlightIntensityMask maskA,
            FanlightIntensityMask maskB,
            FanlightIntensityMask maskC,
            Vector3 maskWeights)
        {
            baseIntensity = FanlightStateValidation.RequireMinimum(baseIntensity, 0f, nameof(baseIntensity));
            randomIntensity = FanlightStateValidation.RequireRange(randomIntensity, 0f, 1f, nameof(randomIntensity));
            maskWeights = NormalizeWeights(maskWeights);
            if (maskWeights.x > 0f) maskA = maskA.Validated();
            if (maskWeights.y > 0f) maskB = maskB.Validated();
            if (maskWeights.z > 0f) maskC = maskC.Validated();

            return new FanlightIntensityState
            {
                _baseIntensity = baseIntensity,
                _randomIntensity = randomIntensity,
                _spatialMask = maskA,
                _secondarySpatialMask = maskB,
                _tertiarySpatialMask = maskC,
                _maskWeights = maskWeights
            };
        }

        internal FanlightIntensityMask GetSpatialMask(int index) => index switch
        {
            0 => _spatialMask,
            1 => _secondarySpatialMask,
            2 => _tertiarySpatialMask,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal float GetSpatialMaskWeight(int index)
        {
            var weights = NormalizeWeights(_maskWeights);
            return index switch
            {
                0 => weights.x,
                1 => weights.y,
                2 => weights.z,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        internal FanlightIntensityState Validated()
        {
            return BlendMasks(
                _baseIntensity,
                _randomIntensity,
                GetSpatialMask(0),
                GetSpatialMask(1),
                GetSpatialMask(2),
                new Vector3(GetSpatialMaskWeight(0), GetSpatialMaskWeight(1), GetSpatialMaskWeight(2)));
        }

        internal bool MaskContentEquals(in FanlightIntensityState other)
        {
            for (var i = 0; i < 3; i++)
            {
                if (!GetSpatialMaskWeight(i).Equals(other.GetSpatialMaskWeight(i))) return false;
                if (GetSpatialMaskWeight(i) > 0f
                    && !GetSpatialMask(i).ContentEquals(other.GetSpatialMask(i)))
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 NormalizeWeights(Vector3 weights)
        {
            if (!FanlightStateValidation.IsFinite(weights)) throw new ArgumentOutOfRangeException(nameof(weights));
            weights.x = Mathf.Max(0f, weights.x);
            weights.y = Mathf.Max(0f, weights.y);
            weights.z = Mathf.Max(0f, weights.z);
            var total = weights.x + weights.y + weights.z;
            if (total <= 0.000001f) return new Vector3(1f, 0f, 0f);
            return weights / total;
        }
    }
}
