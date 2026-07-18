using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightAudienceBodyState
    {
        [SerializeField] private float _height;
        [SerializeField] private float _heightVariation;
        [SerializeField] private float _width;
        [SerializeField] private float _headSize;
        [SerializeField] private float _shoulderHeightRatio;
        [SerializeField] private float _shoulderSideOffset;
        [SerializeField] private float _armWidth;
        [SerializeField] private float _armLengthLimit;
        [SerializeField] private float _upperBodyLeanMaximumRadians;
        [SerializeField] private float _upperBodyLean;
        [SerializeField] private float _bounce;
        [SerializeField] private float _sway;
        [SerializeField] private float _motionSpeed;
        [SerializeField] private float _leanMotion;

        internal float Height => _height;
        internal float HeightVariation => _heightVariation;
        internal float Width => _width;
        internal float HeadSize => _headSize;
        internal float ShoulderHeightRatio => _shoulderHeightRatio;
        internal float ShoulderSideOffset => _shoulderSideOffset;
        internal float ArmWidth => _armWidth;
        internal float ArmLengthLimit => _armLengthLimit;
        internal float UpperBodyLeanMaximumRadians => _upperBodyLeanMaximumRadians;
        internal float UpperBodyLean => _upperBodyLean;
        internal float Bounce => _bounce;
        internal float Sway => _sway;
        internal float MotionSpeed => _motionSpeed;
        internal float LeanMotion => _leanMotion;

        internal FanlightAudienceBodyState(
            float height,
            float heightVariation,
            float width,
            float headSize,
            float shoulderHeightRatio,
            float shoulderSideOffset,
            float armWidth,
            float armLengthLimit,
            float upperBodyLeanMaximumRadians,
            float upperBodyLean,
            float bounce,
            float sway,
            float motionSpeed,
            float leanMotion)
        {
            _height = FanlightStateValidation.RequireRange(height, 0.1f, 3f, nameof(height));
            _heightVariation = FanlightStateValidation.RequireRange(heightVariation, 0f, 1f, nameof(heightVariation));
            _width = FanlightStateValidation.RequireRange(width, 0.01f, 3f, nameof(width));
            _headSize = FanlightStateValidation.RequireRange(headSize, 0.01f, 1f, nameof(headSize));
            _shoulderHeightRatio = FanlightStateValidation.RequireRange(shoulderHeightRatio, 0f, 1f, nameof(shoulderHeightRatio));
            _shoulderSideOffset = FanlightStateValidation.RequireRange(shoulderSideOffset, -1f, 1f, nameof(shoulderSideOffset));
            _armWidth = FanlightStateValidation.RequireRange(armWidth, 0.01f, 1f, nameof(armWidth));
            _armLengthLimit = FanlightStateValidation.RequireRange(armLengthLimit, 0.01f, 3f, nameof(armLengthLimit));
            _upperBodyLeanMaximumRadians = FanlightStateValidation.RequireRange(upperBodyLeanMaximumRadians, 0f, Mathf.PI * 0.5f, nameof(upperBodyLeanMaximumRadians));
            _upperBodyLean = FanlightStateValidation.RequireRange(upperBodyLean, 0f, 1f, nameof(upperBodyLean));
            _bounce = FanlightStateValidation.RequireRange(bounce, 0f, 1f, nameof(bounce));
            _sway = FanlightStateValidation.RequireRange(sway, 0f, 1f, nameof(sway));
            _motionSpeed = FanlightStateValidation.RequireRange(motionSpeed, 0.01f, 16f, nameof(motionSpeed));
            _leanMotion = FanlightStateValidation.RequireRange(leanMotion, 0f, 1f, nameof(leanMotion));
        }
    }
}
