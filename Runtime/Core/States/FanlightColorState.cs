using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightColorState
    {
        // Fields

        [SerializeField]
        private FanlightColorSource _source;

        [NonSerialized]
        private FanlightColorSource _secondarySource;

        [NonSerialized]
        private FanlightColorSource _tertiarySource;

        [NonSerialized]
        private Vector3 _sourceWeights;


        // Properties

        internal FanlightColorSource Source => _source;


        // Methods

        internal FanlightColorState(FanlightColorSource source)
        {
            _source = source.Validated();
            _secondarySource = default;
            _tertiarySource = default;
            _sourceWeights = new Vector3(1f, 0f, 0f);
        }

        internal static FanlightColorState BlendSources(
            FanlightColorSource sourceA,
            FanlightColorSource sourceB,
            FanlightColorSource sourceC,
            Vector3 sourceWeights)
        {
            sourceWeights = NormalizeWeights(sourceWeights);
            if (sourceWeights.x > 0f) sourceA = sourceA.Validated();
            if (sourceWeights.y > 0f) sourceB = sourceB.Validated();
            if (sourceWeights.z > 0f) sourceC = sourceC.Validated();

            return new FanlightColorState
            {
                _source = sourceA,
                _secondarySource = sourceB,
                _tertiarySource = sourceC,
                _sourceWeights = sourceWeights
            };
        }

        internal FanlightColorSource GetSource(int index) => index switch
        {
            0 => _source,
            1 => _secondarySource,
            2 => _tertiarySource,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal float GetSourceWeight(int index)
        {
            var weights = NormalizeWeights(_sourceWeights);
            return index switch
            {
                0 => weights.x,
                1 => weights.y,
                2 => weights.z,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        internal FanlightColorState Validated()
        {
            return BlendSources(
                GetSource(0),
                GetSource(1),
                GetSource(2),
                new Vector3(GetSourceWeight(0), GetSourceWeight(1), GetSourceWeight(2)));
        }

        internal bool ContentEquals(in FanlightColorState other)
        {
            for (var i = 0; i < 3; i++)
            {
                if (!GetSourceWeight(i).Equals(other.GetSourceWeight(i))) return false;
                if (GetSourceWeight(i) > 0f && !GetSource(i).ContentEquals(other.GetSource(i))) return false;
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
