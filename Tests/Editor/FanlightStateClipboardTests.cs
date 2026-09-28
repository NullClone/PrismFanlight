using NUnit.Framework;
using PrismFanlight.Core;
using PrismFanlight.Editor;
using PrismFanlight.Live;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Tests
{
    public sealed class FanlightStateClipboardTests
    {
        // Fields

        private FanlightLiveLook _source;
        private FanlightLiveLook _destination;


        // Methods

        [SetUp]
        public void SetUp()
        {
            _source = ScriptableObject.CreateInstance<FanlightLiveLook>();
            _destination = ScriptableObject.CreateInstance<FanlightLiveLook>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_destination);
        }

        [Test]
        public void Paste_CopiesPatchableFieldsOnly()
        {
            SetFloat(_source, "_noise._phaseAmount", 1.5f);
            SetFloat(_source, "_noise._phaseRate", 2f);
            SetFloat(_destination, "_noise._phaseRate", 8f);

            Assert.That(FanlightStateClipboard.Copy(new Object[] { _source }, FanlightStateKind.Noise, "_noise"), Is.True);
            Assert.That(FanlightStateClipboard.Paste(new Object[] { _destination }, FanlightStateKind.Noise, "_noise"), Is.True);

            Assert.That(GetFloat(_destination, "_noise._phaseAmount"), Is.EqualTo(1.5f));
            Assert.That(GetFloat(_destination, "_noise._phaseRate"), Is.EqualTo(8f));
        }

        [Test]
        public void Paste_CopiesMotionSource()
        {
            SetFloat(_source, "_motion._beatsPerCycle", 3f);
            SetFloat(_source, "_motion._blockDelayXBeats", 0.5f);

            FanlightStateClipboard.Copy(new Object[] { _source }, FanlightStateKind.Motion, "_motion");
            FanlightStateClipboard.Paste(new Object[] { _destination }, FanlightStateKind.Motion, "_motion");

            Assert.That(GetFloat(_destination, "_motion._beatsPerCycle"), Is.EqualTo(3f));
            Assert.That(GetFloat(_destination, "_motion._blockDelayXBeats"), Is.EqualTo(0.5f));
        }

        [Test]
        public void CanPaste_DifferentKind_ReturnsFalse()
        {
            FanlightStateClipboard.Copy(new Object[] { _source }, FanlightStateKind.Intent, "_intent");

            Assert.That(FanlightStateClipboard.CanPaste(FanlightStateKind.Intent), Is.True);
            Assert.That(FanlightStateClipboard.CanPaste(FanlightStateKind.Rest), Is.False);
            Assert.That(FanlightStateClipboard.Paste(new Object[] { _destination }, FanlightStateKind.Rest, "_rest"), Is.False);
        }

        [Test]
        public void Copy_MultipleTargets_ReturnsFalse()
        {
            Assert.That(FanlightStateClipboard.Copy(new Object[] { _source, _destination }, FanlightStateKind.Intent, "_intent"), Is.False);
        }


        private static void SetFloat(Object target, string propertyPath, float value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyPath).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float GetFloat(Object target, string propertyPath)
        {
            return new SerializedObject(target).FindProperty(propertyPath).floatValue;
        }
    }
}
