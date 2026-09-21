using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightMotionState
    {
        // Fields

        [SerializeField]
        private FanlightMotionAsset _motionAsset;

        [Space]
        [SerializeField]
        private float _beatsPerCycle;

        [SerializeField]
        private float _phaseOffsetBeats;

        [SerializeField]
        private float _blockDelayXBeats;

        [SerializeField]
        private float _blockDelayYBeats;


        [NonSerialized]
        private FanlightMotionAsset _secondaryMotionAsset;

        [NonSerialized]
        private FanlightMotionAsset _tertiaryMotionAsset;

        [NonSerialized]
        private Vector3 _assetWeights;


        // Properties

        internal FanlightMotionAsset MotionAsset => _motionAsset;

        internal float BeatsPerCycle => _beatsPerCycle;

        internal float PhaseOffsetBeats => _phaseOffsetBeats;

        internal float BlockDelayXBeats => _blockDelayXBeats;

        internal float BlockDelayYBeats => _blockDelayYBeats;

        // Methods

        internal FanlightMotionState(
            FanlightMotionAsset motionAsset,
            float beatsPerCycle,
            float phaseOffsetBeats,
            float blockDelayXBeats,
            float blockDelayYBeats)
        {
            _motionAsset = motionAsset;
            _beatsPerCycle = FanlightStateValidation.RequireRange(beatsPerCycle, 0.001f, 64f, nameof(beatsPerCycle));
            _phaseOffsetBeats = FanlightStateValidation.RequireRange(phaseOffsetBeats, -64f, 64f, nameof(phaseOffsetBeats));
            _blockDelayXBeats = FanlightStateValidation.RequireRange(blockDelayXBeats, -64f, 64f, nameof(blockDelayXBeats));
            _blockDelayYBeats = FanlightStateValidation.RequireRange(blockDelayYBeats, -64f, 64f, nameof(blockDelayYBeats));
            _secondaryMotionAsset = null;
            _tertiaryMotionAsset = null;
            _assetWeights = new Vector3(1f, 0f, 0f);
        }

        internal static FanlightMotionState BlendAssets(
            FanlightMotionAsset assetA,
            FanlightMotionAsset assetB,
            FanlightMotionAsset assetC,
            Vector3 assetWeights,
            float beatsPerCycle,
            float phaseOffsetBeats,
            float blockDelayXBeats,
            float blockDelayYBeats)
        {
            var state = new FanlightMotionState(
                assetA,
                beatsPerCycle,
                phaseOffsetBeats,
                blockDelayXBeats,
                blockDelayYBeats)
            {
                _secondaryMotionAsset = assetB,
                _tertiaryMotionAsset = assetC,
                _assetWeights = NormalizeWeights(assetWeights)
            };
            return state;
        }

        internal FanlightMotionAsset GetAsset(int index) => index switch
        {
            0 => _motionAsset,
            1 => _secondaryMotionAsset,
            2 => _tertiaryMotionAsset,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal float GetAssetWeight(int index)
        {
            var weights = NormalizeWeights(_assetWeights);
            return index switch
            {
                0 => weights.x,
                1 => weights.y,
                2 => weights.z,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        internal bool HasValidAssets()
        {
            for (var i = 0; i < 3; i++)
            {
                if (GetAssetWeight(i) <= 0f) continue;
                var asset = GetAsset(i);
                if (asset == null || !asset.HasValidBake) return false;
            }

            return true;
        }

        private static Vector3 NormalizeWeights(Vector3 weights)
        {
            if (!FanlightStateValidation.IsFinite(weights)) return new Vector3(1f, 0f, 0f);
            weights.x = Mathf.Max(0f, weights.x);
            weights.y = Mathf.Max(0f, weights.y);
            weights.z = Mathf.Max(0f, weights.z);
            var total = weights.x + weights.y + weights.z;
            return total > 0.000001f ? weights / total : new Vector3(1f, 0f, 0f);
        }
    }
}
