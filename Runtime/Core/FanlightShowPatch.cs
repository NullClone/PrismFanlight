using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightShowPatch
    {
        [SerializeField] private FanlightIntentPatch _intent;
        [SerializeField] private FanlightGesturePatch _gesture;
        [SerializeField] private FanlightPosePatch _pose;
        [SerializeField] private FanlightVariationPatch _variation;
        [SerializeField] private FanlightNoisePatch _noise;
        [SerializeField] private FanlightRestPatch _rest;
        [SerializeField] private FanlightAudienceBodyPatch _audienceBody;
        [SerializeField] private FanlightDirectionPatch _direction;
        [SerializeField] private FanlightPalettePatch _palette;
        [SerializeField] private FanlightVisibilityPatch _visibility;

        internal FanlightIntentPatch Intent => _intent;
        internal FanlightGesturePatch Gesture => _gesture;
        internal FanlightPosePatch Pose => _pose;
        internal FanlightVariationPatch Variation => _variation;
        internal FanlightNoisePatch Noise => _noise;
        internal FanlightRestPatch Rest => _rest;
        internal FanlightAudienceBodyPatch AudienceBody => _audienceBody;
        internal FanlightDirectionPatch Direction => _direction;
        internal FanlightPalettePatch Palette => _palette;
        internal FanlightVisibilityPatch Visibility => _visibility;

        internal FanlightShowPatch(
            FanlightIntentPatch intent,
            FanlightGesturePatch gesture,
            FanlightPosePatch pose,
            FanlightVariationPatch variation,
            FanlightNoisePatch noise,
            FanlightRestPatch rest,
            FanlightAudienceBodyPatch audienceBody,
            FanlightDirectionPatch direction,
            FanlightPalettePatch palette,
            FanlightVisibilityPatch visibility)
        {
            _intent = intent;
            _gesture = gesture;
            _pose = pose;
            _variation = variation;
            _noise = noise;
            _rest = rest;
            _audienceBody = audienceBody;
            _direction = direction;
            _palette = palette;
            _visibility = visibility;
        }
    }
}
