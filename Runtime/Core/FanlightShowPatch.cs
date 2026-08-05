using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightShowPatch
    {
        // Fields

        [SerializeField]
        private FanlightIntentPatch _intent;

        [SerializeField]
        private FanlightMotionPatch _motion;

        [SerializeField]
        private FanlightVariationPatch _variation;

        [SerializeField]
        private FanlightNoisePatch _noise;

        [SerializeField]
        private FanlightRestPatch _rest;

        [SerializeField]
        private FanlightAudienceBodyPatch _audienceBody;

        [SerializeField]
        private FanlightDirectionPatch _direction;

        [SerializeField]
        private FanlightColorPatch _color;

        [SerializeField]
        private FanlightIntensityPatch _intensity;


        // Properties

        internal FanlightIntentPatch Intent => _intent;

        internal FanlightMotionPatch Motion => _motion;

        internal FanlightVariationPatch Variation => _variation;

        internal FanlightNoisePatch Noise => _noise;

        internal FanlightRestPatch Rest => _rest;

        internal FanlightAudienceBodyPatch AudienceBody => _audienceBody;

        internal FanlightDirectionPatch Direction => _direction;

        internal FanlightColorPatch Color => _color;

        internal FanlightIntensityPatch Intensity => _intensity;


        // Methods

        internal FanlightShowPatch(
            FanlightIntentPatch intent,
            FanlightMotionPatch motion,
            FanlightVariationPatch variation,
            FanlightNoisePatch noise,
            FanlightRestPatch rest,
            FanlightAudienceBodyPatch audienceBody,
            FanlightDirectionPatch direction,
            FanlightColorPatch color,
            FanlightIntensityPatch intensity)
        {
            _intent = intent;
            _motion = motion;
            _variation = variation;
            _noise = noise;
            _rest = rest;
            _audienceBody = audienceBody;
            _direction = direction;
            _color = color;
            _intensity = intensity;
        }
    }
}
