namespace PrismFanlight.Core
{
    internal sealed class FanlightShowPatchBuilder
    {
        // Fields

        private FanlightIntentState _intent;
        private FanlightIntentFields _intentFields;
        private FanlightGestureState _gesture;
        private FanlightGestureFields _gestureFields;
        private FanlightPoseState _pose;
        private FanlightPoseFields _poseFields;
        private FanlightVariationState _variation;
        private FanlightVariationFields _variationFields;
        private FanlightNoiseState _noise;
        private FanlightNoiseFields _noiseFields;
        private FanlightRestState _rest;
        private FanlightRestFields _restFields;
        private FanlightAudienceBodyState _audienceBody;
        private FanlightAudienceBodyFields _audienceBodyFields;
        private FanlightDirectionState _direction;
        private FanlightDirectionFields _directionFields;
        private FanlightPaletteState _palette;
        private FanlightPaletteFields _paletteFields;
        private FanlightVisibilityState _visibility;
        private FanlightVisibilityFields _visibilityFields;


        // Methods

        internal FanlightShowPatchBuilder(FanlightShowState baseState)
        {
            _intent = baseState.Intent;
            _gesture = baseState.Gesture;
            _pose = baseState.Pose;
            _variation = baseState.Variation;
            _noise = baseState.Noise;
            _rest = baseState.Rest;
            _audienceBody = baseState.AudienceBody;
            _direction = baseState.Direction;
            _palette = baseState.Palette;
            _visibility = baseState.Visibility;
        }

        internal void SetIntent(FanlightIntentFields fields, FanlightIntentState value)
        {
            _intent = FanlightShowStatePatcher.Apply(_intent, new FanlightIntentPatch(fields, value), 1f);
            _intentFields |= fields;
        }

        internal void SetGesture(FanlightGestureFields fields, FanlightGestureState value)
        {
            _gesture = FanlightShowStatePatcher.Apply(_gesture, new FanlightGesturePatch(fields, value), 1f);
            _gestureFields |= fields;
        }

        internal void SetPose(FanlightPoseFields fields, FanlightPoseState value)
        {
            _pose = FanlightShowStatePatcher.Apply(_pose, new FanlightPosePatch(fields, value), 1f);
            _poseFields |= fields;
        }

        internal void SetVariation(FanlightVariationFields fields, FanlightVariationState value)
        {
            _variation = FanlightShowStatePatcher.Apply(_variation, new FanlightVariationPatch(fields, value), 1f);
            _variationFields |= fields;
        }

        internal void SetNoise(FanlightNoiseFields fields, FanlightNoiseState value)
        {
            _noise = FanlightShowStatePatcher.Apply(_noise, new FanlightNoisePatch(fields, value), 1f);
            _noiseFields |= fields;
        }

        internal void SetRest(FanlightRestFields fields, FanlightRestState value)
        {
            _rest = FanlightShowStatePatcher.Apply(_rest, new FanlightRestPatch(fields, value), 1f);
            _restFields |= fields;
        }

        internal void SetAudienceBody(FanlightAudienceBodyFields fields, FanlightAudienceBodyState value)
        {
            _audienceBody = FanlightShowStatePatcher.Apply(_audienceBody, new FanlightAudienceBodyPatch(fields, value), 1f);
            _audienceBodyFields |= fields;
        }

        internal void SetDirection(FanlightDirectionFields fields, FanlightDirectionState value)
        {
            _direction = FanlightShowStatePatcher.Apply(_direction, new FanlightDirectionPatch(fields, value), 1f);
            _directionFields |= fields;
        }

        internal void SetPalette(FanlightPaletteFields fields, FanlightPaletteState value)
        {
            _palette = FanlightShowStatePatcher.Apply(_palette, new FanlightPalettePatch(fields, value), 1f);
            _paletteFields |= fields;
        }

        internal void SetVisibility(FanlightVisibilityFields fields, FanlightVisibilityState value)
        {
            _visibility = FanlightShowStatePatcher.Apply(_visibility, new FanlightVisibilityPatch(fields, value), 1f);
            _visibilityFields |= fields;
        }

        internal FanlightShowPatch Build() => new(
            new FanlightIntentPatch(_intentFields, _intent),
            new FanlightGesturePatch(_gestureFields, _gesture),
            new FanlightPosePatch(_poseFields, _pose),
            new FanlightVariationPatch(_variationFields, _variation),
            new FanlightNoisePatch(_noiseFields, _noise),
            new FanlightRestPatch(_restFields, _rest),
            new FanlightAudienceBodyPatch(_audienceBodyFields, _audienceBody),
            new FanlightDirectionPatch(_directionFields, _direction),
            new FanlightPalettePatch(_paletteFields, _palette),
            new FanlightVisibilityPatch(_visibilityFields, _visibility));
    }
}
