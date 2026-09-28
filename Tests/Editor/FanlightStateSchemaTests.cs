using System;
using NUnit.Framework;
using PrismFanlight.Core;
using PrismFanlight.Editor;

namespace PrismFanlight.Tests
{
    public sealed class FanlightStateSchemaTests
    {
        // Methods

        [Test]
        public void CollectErrors_CurrentStates_ReturnsNoErrors()
        {
            Assert.That(FanlightStateSchema.CollectErrors(), Is.Empty);
        }

        [Test]
        public void GetPatchableFields_CoverAllFields()
        {
            foreach (FanlightStateKind kind in Enum.GetValues(typeof(FanlightStateKind)))
            {
                var covered = 0;
                foreach (var field in FanlightStateSchema.GetPatchableFields(kind))
                {
                    covered |= field.Bit;
                }

                Assert.That(covered, Is.EqualTo(FanlightStateSchema.GetAllFields(kind)), kind.ToString());
            }
        }

        [Test]
        public void GetAllFields_MatchesFieldsAll()
        {
            Assert.That(FanlightStateSchema.GetAllFields(FanlightStateKind.Intent), Is.EqualTo((int)FanlightIntentFields.All));
            Assert.That(FanlightStateSchema.GetAllFields(FanlightStateKind.Motion), Is.EqualTo((int)FanlightMotionFields.All));
            Assert.That(FanlightStateSchema.GetAllFields(FanlightStateKind.Intensity), Is.EqualTo((int)FanlightIntensityFields.All));
        }

        [Test]
        public void GetField_MotionSource_CoversAssetAndCycle()
        {
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Motion, "_motionAsset").Bit, Is.EqualTo((int)FanlightMotionFields.Source));
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Motion, "_beatsPerCycle").Bit, Is.EqualTo((int)FanlightMotionFields.Source));
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Motion, "_phaseOffsetBeats").Bit, Is.EqualTo((int)FanlightMotionFields.Source));
        }

        [Test]
        public void GetField_NoiseBaselineField_IsNotPatchable()
        {
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Noise, "_phaseRate").IsPatchable, Is.False);
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Noise, "_octaves").IsPatchable, Is.False);
            Assert.That(FanlightStateSchema.GetPatchableFields(FanlightStateKind.Noise).Count, Is.EqualTo(3));
        }

        [Test]
        public void GetField_Label_ComesFromAttribute()
        {
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Variation, "_heightVariation").Label.text, Is.EqualTo("Audience Height"));
            Assert.That(FanlightStateSchema.GetField(FanlightStateKind.Intent, "_energy").Label, Is.Null);
        }

        [Test]
        public void GetField_UnknownField_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => FanlightStateSchema.GetField(FanlightStateKind.Intent, "_unknown"));
        }

        [Test]
        public void IsIncluded_UsesFieldBit()
        {
            var field = FanlightStateSchema.GetField(FanlightStateKind.Intent, "_participation");

            Assert.That(field.IsIncluded((int)FanlightIntentFields.Participation), Is.True);
            Assert.That(field.IsIncluded((int)FanlightIntentFields.Energy), Is.False);
        }
    }
}
