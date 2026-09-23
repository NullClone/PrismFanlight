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

        [SerializeField, Range(0f, 1f)]
        private float _randomIntensity;

        [SerializeField]
        private FanlightIntensityMask _mask;

        [NonSerialized]
        private FanlightIntensityMask _secondaryMask;

        [NonSerialized]
        private FanlightIntensityMask _tertiaryMask;

        [NonSerialized]
        private Vector3 _maskWeights;


        // Properties

        internal float BaseIntensity => _baseIntensity;

        internal float RandomIntensity => _randomIntensity;

        internal FanlightIntensityMask Mask => _mask;

        internal float ResolvedLocalYawDegrees => ResolveLocalYawDegrees();


        // Methods

        internal FanlightIntensityState(
            float baseIntensity,
            float randomIntensity,
            FanlightIntensityMask mask)
        {
            _baseIntensity = FanlightStateValidation.RequireMinimum(baseIntensity, 0f, nameof(baseIntensity));
            _randomIntensity = FanlightStateValidation.RequireRange(randomIntensity, 0f, 1f, nameof(randomIntensity));
            _mask = mask.Validated();
            _secondaryMask = default;
            _tertiaryMask = default;
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
                _mask = maskA,
                _secondaryMask = maskB,
                _tertiaryMask = maskC,
                _maskWeights = maskWeights
            };
        }

        internal FanlightIntensityMask GetMask(int index) => index switch
        {
            0 => _mask,
            1 => _secondaryMask,
            2 => _tertiaryMask,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal float GetMaskWeight(int index)
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
                GetMask(0),
                GetMask(1),
                GetMask(2),
                new Vector3(GetMaskWeight(0), GetMaskWeight(1), GetMaskWeight(2)));
        }

        internal bool MaskContentEquals(in FanlightIntensityState other)
        {
            for (var i = 0; i < 3; i++)
            {
                if (!GetMaskWeight(i).Equals(other.GetMaskWeight(i))) return false;
                if (GetMaskWeight(i) > 0f
                    && !GetMask(i).ContentEquals(other.GetMask(i)))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool HasDynamicMask()
        {
            for (var i = 0; i < 3; i++)
            {
                if (GetMaskWeight(i) > 0f
                    && GetMask(i).Mode != FanlightIntensityMaskMode.None)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasRandomSparkleMask()
        {
            for (var i = 0; i < 3; i++)
            {
                if (GetMaskWeight(i) > 0f
                    && GetMask(i).Mode == FanlightIntensityMaskMode.RandomSparkle)
                {
                    return true;
                }
            }

            return false;
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

        private float ResolveLocalYawDegrees()
        {
            var resolvedYaw = 0f;
            var accumulatedWeight = 0d;

            for (var i = 0; i < 3; i++)
            {
                var mask = GetMask(i);
                var weight = GetMaskWeight(i);
                if (weight <= 0f || !mask.UsesLocalYaw) continue;

                resolvedYaw = accumulatedWeight <= 0d
                    ? mask.LocalYawDegrees
                    : FanlightStateValidation.LerpShortestDegrees(
                        resolvedYaw,
                        mask.LocalYawDegrees,
                        (float)(weight / (accumulatedWeight + weight)));
                accumulatedWeight += weight;
            }

            return resolvedYaw;
        }
    }
}
