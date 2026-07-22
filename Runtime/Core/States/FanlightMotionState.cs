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

        [SerializeField]
        private float _beatsPerCycle;

        [SerializeField]
        private float _phaseOffsetBeats;

        [SerializeField]
        private float _motionAmount;

        [SerializeField]
        private float _heightBias;

        [SerializeField]
        private float _sideScale;

        [SerializeField]
        private float _forwardScale;

        [SerializeField]
        private float _wristDelayRatio;

        [SerializeField]
        private float _variation;

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

        internal float MotionAmount => _motionAmount;

        internal float HeightBias => _heightBias;

        internal float SideScale => _sideScale;

        internal float ForwardScale => _forwardScale;

        internal float WristDelayRatio => _wristDelayRatio;

        internal float Variation => _variation;


        // Methods

        internal FanlightMotionState(
            FanlightMotionAsset motionAsset,
            float beatsPerCycle,
            float phaseOffsetBeats,
            float motionAmount,
            float heightBias,
            float sideScale,
            float forwardScale,
            float wristDelayRatio,
            float variation)
        {
            _motionAsset = motionAsset;
            _beatsPerCycle = FanlightStateValidation.RequireRange(beatsPerCycle, 0.001f, 64f, nameof(beatsPerCycle));
            _phaseOffsetBeats = FanlightStateValidation.RequireRange(phaseOffsetBeats, -64f, 64f, nameof(phaseOffsetBeats));
            _motionAmount = FanlightStateValidation.RequireRange(motionAmount, 0f, 2f, nameof(motionAmount));
            _heightBias = FanlightStateValidation.RequireRange(heightBias, -1f, 1f, nameof(heightBias));
            _sideScale = FanlightStateValidation.RequireRange(sideScale, 0f, 2f, nameof(sideScale));
            _forwardScale = FanlightStateValidation.RequireRange(forwardScale, 0f, 2f, nameof(forwardScale));
            _wristDelayRatio = FanlightStateValidation.RequireRange(wristDelayRatio, 0f, 0.5f, nameof(wristDelayRatio));
            _variation = FanlightStateValidation.RequireRange(variation, 0f, 1f, nameof(variation));
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
            float motionAmount,
            float heightBias,
            float sideScale,
            float forwardScale,
            float wristDelayRatio,
            float variation)
        {
            var state = new FanlightMotionState(
                assetA,
                beatsPerCycle,
                phaseOffsetBeats,
                motionAmount,
                heightBias,
                sideScale,
                forwardScale,
                wristDelayRatio,
                variation)
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
