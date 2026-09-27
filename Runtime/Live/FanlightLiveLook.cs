using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Live
{
    [HelpURL(PrismFanlight.HelpUrl)]
    public sealed class FanlightLiveLook : ScriptableObject
    {
        // Fields

        [SerializeField]
        private FanlightLiveQuantize _quantize = FanlightLiveQuantize.Beat;

        [SerializeField]
        private FanlightIntentFields _intentFields;

        [SerializeField]
        private FanlightIntentState _intent = FanlightShowStateDefaults.Intent();

        [SerializeField]
        private FanlightMotionFields _motionFields;

        [SerializeField]
        private FanlightMotionState _motion = FanlightShowStateDefaults.Motion();

        [SerializeField]
        private FanlightVariationFields _variationFields;

        [SerializeField]
        private FanlightVariationState _variation = FanlightShowStateDefaults.Variation();

        [SerializeField]
        private FanlightNoiseFields _noiseFields;

        [SerializeField]
        private FanlightNoiseState _noise = FanlightShowStateDefaults.Noise();

        [SerializeField]
        private FanlightRestFields _restFields;

        [SerializeField]
        private FanlightRestState _rest = FanlightShowStateDefaults.Rest();

        [SerializeField]
        private FanlightAudienceBodyFields _audienceBodyFields;

        [SerializeField]
        private FanlightAudienceBodyState _audienceBody = FanlightShowStateDefaults.AudienceBody();

        [SerializeField]
        private FanlightDirectionFields _directionFields;

        [SerializeField]
        private FanlightDirectionState _direction = FanlightShowStateDefaults.Direction();

        [SerializeField]
        private FanlightColorFields _colorFields;

        [SerializeField]
        private FanlightColorState _color = FanlightShowStateDefaults.Color();

        [SerializeField]
        private FanlightIntensityFields _intensityFields;

        [SerializeField]
        private FanlightIntensityState _intensity = FanlightShowStateDefaults.Intensity();


        // Properties

        internal FanlightLiveQuantize Quantize => _quantize;

        internal FanlightShowPatch Patch
        {
            get
            {
                if ((_motionFields & FanlightMotionFields.Source) != 0
                    && (_motion.MotionAsset == null || !_motion.MotionAsset.HasValidBake))
                {
                    throw new InvalidOperationException("Looks that set the Motion source require a baked Motion Asset.");
                }

                return new FanlightShowPatch(
                    new FanlightIntentPatch(_intentFields, _intent),
                    new FanlightMotionPatch(_motionFields, _motion),
                    new FanlightVariationPatch(_variationFields, _variation),
                    new FanlightNoisePatch(_noiseFields, _noise),
                    new FanlightRestPatch(_restFields, _rest),
                    new FanlightAudienceBodyPatch(_audienceBodyFields, _audienceBody),
                    new FanlightDirectionPatch(_directionFields, _direction),
                    new FanlightColorPatch(_colorFields, _colorFields != FanlightColorFields.None ? _color.Validated() : _color),
                    new FanlightIntensityPatch(_intensityFields, _intensityFields != FanlightIntensityFields.None ? _intensity.Validated() : _intensity));
            }
        }


        // Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            _intent = FanlightShowStateAuthoringValidator.Validate(_intent);
            _motion = FanlightShowStateAuthoringValidator.Validate(_motion);
            _variation = FanlightShowStateAuthoringValidator.Validate(_variation);
            _noise = FanlightShowStateAuthoringValidator.Validate(_noise);
            _rest = FanlightShowStateAuthoringValidator.Validate(_rest);
            _audienceBody = FanlightShowStateAuthoringValidator.Validate(_audienceBody);
            _direction = FanlightShowStateAuthoringValidator.Validate(_direction);
        }
#endif
    }
}
