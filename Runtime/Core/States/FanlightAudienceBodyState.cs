using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightAudienceBodyState
    {
        // Fields

        [SerializeField, FanlightField(FanlightAudienceBodyFields.Height, "Body Height")]
        private float _height;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.Width, "Body Width")]
        private float _width;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.HeadSize)]
        private float _headSize;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.ArmWidth)]
        private float _armWidth;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.ArmLengthLimit)]
        private float _armLengthLimit;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.ShoulderHeightRatio, "Shoulder Height"), Range(0f, 1f)]
        private float _shoulderHeightRatio;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.ShoulderSideOffset, "Shoulder Offset"), Range(-1f, 1f)]
        private float _shoulderSideOffset;

        [Space]
        [SerializeField, FanlightField(FanlightAudienceBodyFields.Bounce), Range(0f, 1f)]
        private float _bounce;

        [SerializeField, FanlightField(FanlightAudienceBodyFields.Sway), Range(0f, 1f)]
        private float _sway;


        // Properties

        internal float Height => _height;

        internal float Width => _width;

        internal float HeadSize => _headSize;

        internal float ShoulderHeightRatio => _shoulderHeightRatio;

        internal float ShoulderSideOffset => _shoulderSideOffset;

        internal float ArmWidth => _armWidth;

        internal float ArmLengthLimit => _armLengthLimit;

        internal float Bounce => _bounce;

        internal float Sway => _sway;


        // Methods

        internal FanlightAudienceBodyState(
            float height,
            float width,
            float headSize,
            float shoulderHeightRatio,
            float shoulderSideOffset,
            float armWidth,
            float armLengthLimit,
            float bounce,
            float sway)
        {
            _height = FanlightStateValidation.RequireRange(height, 0.1f, 3f, nameof(height));
            _width = FanlightStateValidation.RequireRange(width, 0.01f, 3f, nameof(width));
            _headSize = FanlightStateValidation.RequireRange(headSize, 0.01f, 1f, nameof(headSize));
            _shoulderHeightRatio = FanlightStateValidation.RequireRange(shoulderHeightRatio, 0f, 1f, nameof(shoulderHeightRatio));
            _shoulderSideOffset = FanlightStateValidation.RequireRange(shoulderSideOffset, -1f, 1f, nameof(shoulderSideOffset));
            _armWidth = FanlightStateValidation.RequireRange(armWidth, 0.01f, 1f, nameof(armWidth));
            _armLengthLimit = FanlightStateValidation.RequireRange(armLengthLimit, 0.01f, 3f, nameof(armLengthLimit));
            _bounce = FanlightStateValidation.RequireRange(bounce, 0f, 1f, nameof(bounce));
            _sway = FanlightStateValidation.RequireRange(sway, 0f, 1f, nameof(sway));
        }
    }
}
