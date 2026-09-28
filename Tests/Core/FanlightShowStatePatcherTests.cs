using System;
using NUnit.Framework;
using PrismFanlight.Core;
using static PrismFanlight.Tests.FanlightTestInputs;

namespace PrismFanlight.Tests
{
    public sealed class FanlightShowStatePatcherTests
    {
        // Fields

        private const float Tolerance = 1e-5f;


        // Methods

        [Test]
        public void Apply_WithoutFields_KeepsState()
        {
            var state = FanlightShowStatePatcher.Apply(BaseState(energy: 0.4f), default, 1f);

            Assert.That(state.Intent.Energy, Is.EqualTo(0.4f).Within(Tolerance));
        }

        [Test]
        public void Apply_WeightAboveOne_IsClamped()
        {
            var state = FanlightShowStatePatcher.Apply(BaseState(energy: 1f), IntentPatch(FanlightIntentFields.Energy, 0.2f), 2f);

            Assert.That(state.Intent.Energy, Is.EqualTo(0.2f).Within(Tolerance));
        }

        [Test]
        public void Apply_NegativeWeight_IsClamped()
        {
            var state = FanlightShowStatePatcher.Apply(BaseState(energy: 1f), IntentPatch(FanlightIntentFields.Energy, 0.2f), -1f);

            Assert.That(state.Intent.Energy, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Apply_UndefinedFieldBit_Throws()
        {
            var patch = IntentPatch((FanlightIntentFields)(1 << 5), 0.2f);

            Assert.Throws<ArgumentOutOfRangeException>(() => FanlightShowStatePatcher.Apply(BaseState(), patch, 1f));
        }

        [Test]
        public void Validate_RestDurationLongerThanCycle_Throws()
        {
            var state = BaseState();
            var invalid = new FanlightShowState(
                state.Intent,
                state.Motion,
                state.Variation,
                state.Noise,
                new FanlightRestState(0.5f, 0f, 1f, 2f, 0f, 0f),
                state.AudienceBody,
                state.Direction,
                state.Color,
                state.Intensity,
                state.Visibility,
                state.GlobalSeed);

            Assert.Throws<InvalidOperationException>(() => FanlightShowStatePatcher.Validate(invalid));
        }

        [Test]
        public void Validate_DefaultState_Succeeds()
        {
            Assert.DoesNotThrow(() => FanlightShowStatePatcher.Validate(BaseState()));
        }
    }
}
