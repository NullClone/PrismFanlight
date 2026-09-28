using System;
using NUnit.Framework;
using PrismFanlight.Core;
using PrismFanlight.Live;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrismFanlight.Tests
{
    public sealed class FanlightLiveLookTests
    {
        // Fields

        private FanlightLiveLook _look;


        // Methods

        [SetUp]
        public void SetUp()
        {
            _look = ScriptableObject.CreateInstance<FanlightLiveLook>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_look);
        }

        [Test]
        public void Patch_WithoutFields_ChangesNothing()
        {
            var patch = _look.Patch;

            Assert.That(patch.Intent.Fields, Is.EqualTo(FanlightIntentFields.None));
            Assert.That(patch.Motion.Fields, Is.EqualTo(FanlightMotionFields.None));
            Assert.That(patch.Intensity.Fields, Is.EqualTo(FanlightIntensityFields.None));
        }

        [Test]
        public void Patch_MotionSourceWithoutBakedAsset_Throws()
        {
            var serializedObject = new SerializedObject(_look);
            serializedObject.FindProperty("_motionFields").intValue = (int)FanlightMotionFields.Source;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.Throws<InvalidOperationException>(() => _ = _look.Patch);
        }

        [Test]
        public void Patch_IncludesSelectedFields()
        {
            var serializedObject = new SerializedObject(_look);
            serializedObject.FindProperty("_intentFields").intValue = (int)FanlightIntentFields.Energy;
            serializedObject.FindProperty("_intent._energy").floatValue = 0.25f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var patch = _look.Patch;

            Assert.That(patch.Intent.Fields, Is.EqualTo(FanlightIntentFields.Energy));
            Assert.That(patch.Intent.Value.Energy, Is.EqualTo(0.25f));
        }
    }
}
