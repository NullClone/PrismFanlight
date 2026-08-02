using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightMotionPresetGenerator
    {
        // Fields

        private const string ReferenceArmElevationName = "_referenceArmElevation";
        private const string ReferenceArmSideName = "_referenceArmSide";
        private const string ReferenceArmExtensionName = "_referenceArmExtension";
        private const string ReferencePenlightElevationName = "_referencePenlightElevation";
        private const string ReferencePenlightSideName = "_referencePenlightSide";
        private const string ReferenceBodyLeanName = "_referenceBodyLean";
        private const string ArmElevationAmplitudeName = "_armElevationAmplitude";
        private const string ArmSideAmplitudeName = "_armSideAmplitude";
        private const string ArmExtensionAmplitudeName = "_armExtensionAmplitude";
        private const string PenlightElevationAmplitudeName = "_penlightElevationAmplitude";
        private const string PenlightSideAmplitudeName = "_penlightSideAmplitude";
        private const string BodyLeanAmplitudeName = "_bodyLeanAmplitude";
        private const string ArmElevationName = "_armElevation";
        private const string ArmSideName = "_armSide";
        private const string ArmExtensionName = "_armExtension";
        private const string PenlightElevationName = "_penlightElevation";
        private const string PenlightSideName = "_penlightSide";
        private const string BodyLeanName = "_bodyLean";


        // Methods

        internal static void GenerateWiper(
            SerializedObject serializedObject,
            float sweepAngle,
            float armElevation,
            float armExtension,
            float penlightElevation)
        {
            sweepAngle = Mathf.Clamp(sweepAngle, 0f, 90f);
            armElevation = Mathf.Clamp(armElevation, -90f, 90f);
            armExtension = Mathf.Clamp01(armExtension);
            penlightElevation = Mathf.Clamp(penlightElevation, -90f, 90f);
            var centerExtension = Mathf.Clamp01(armExtension - 0.04f);
            var penlightSweepAngle = sweepAngle * 0.85f;

            SetFloat(serializedObject, ReferenceArmElevationName, armElevation);
            SetFloat(serializedObject, ReferenceArmSideName, 0f);
            SetFloat(serializedObject, ReferenceArmExtensionName, centerExtension);
            SetFloat(serializedObject, ReferencePenlightElevationName, penlightElevation);
            SetFloat(serializedObject, ReferencePenlightSideName, 0f);
            SetFloat(serializedObject, ReferenceBodyLeanName, 0f);
            SetFloat(serializedObject, ArmElevationAmplitudeName, 0f);
            SetFloat(serializedObject, ArmSideAmplitudeName, sweepAngle);
            SetFloat(serializedObject, ArmExtensionAmplitudeName, armExtension - centerExtension);
            SetFloat(serializedObject, PenlightElevationAmplitudeName, 0f);
            SetFloat(serializedObject, PenlightSideAmplitudeName, penlightSweepAngle);
            SetFloat(serializedObject, BodyLeanAmplitudeName, 0f);
            SetCurve(serializedObject, ArmElevationName, CreateConstantCurve(0f));
            SetCurve(serializedObject, ArmSideName, CreateSweepCurve());
            SetCurve(
                serializedObject,
                ArmExtensionName,
                CreateCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.25f, 0f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(0.75f, 0f),
                    new Keyframe(1f, 1f)));
            SetCurve(serializedObject, PenlightElevationName, CreateConstantCurve(0f));
            SetCurve(serializedObject, PenlightSideName, CreateSweepCurve());
            SetCurve(serializedObject, BodyLeanName, CreateConstantCurve(0f));
        }

        internal static void GenerateSasage(
            SerializedObject serializedObject,
            float lowElevation,
            float highElevation,
            float lowExtension,
            float highExtension,
            float holdRatio)
        {
            lowElevation = Mathf.Clamp(lowElevation, -90f, 90f);
            highElevation = Mathf.Clamp(Mathf.Max(lowElevation, highElevation), -90f, 90f);
            lowExtension = Mathf.Clamp01(lowExtension);
            highExtension = Mathf.Clamp(Mathf.Max(lowExtension, highExtension), 0f, 1f);
            holdRatio = Mathf.Clamp(holdRatio, 0.1f, 0.55f);
            var liftEnd = Mathf.Min(0.4f, (1f - holdRatio) * 0.5f);
            var holdEnd = liftEnd + holdRatio;
            var lowPenlightElevation = Mathf.Clamp(lowElevation + 30f, -90f, 90f);
            var highPenlightElevation = Mathf.Clamp(
                Mathf.Max(lowPenlightElevation, highElevation + 15f),
                -90f,
                90f);

            SetFloat(serializedObject, ReferenceArmElevationName, lowElevation);
            SetFloat(serializedObject, ReferenceArmSideName, 0f);
            SetFloat(serializedObject, ReferenceArmExtensionName, lowExtension);
            SetFloat(serializedObject, ReferencePenlightElevationName, lowPenlightElevation);
            SetFloat(serializedObject, ReferencePenlightSideName, 0f);
            SetFloat(serializedObject, ReferenceBodyLeanName, 0f);
            SetFloat(serializedObject, ArmElevationAmplitudeName, highElevation - lowElevation);
            SetFloat(serializedObject, ArmSideAmplitudeName, 0f);
            SetFloat(serializedObject, ArmExtensionAmplitudeName, highExtension - lowExtension);
            SetFloat(
                serializedObject,
                PenlightElevationAmplitudeName,
                highPenlightElevation - lowPenlightElevation);
            SetFloat(serializedObject, PenlightSideAmplitudeName, 0f);
            SetFloat(serializedObject, BodyLeanAmplitudeName, 3f);
            SetCurve(
                serializedObject,
                ArmElevationName,
                CreateLiftCurve(liftEnd, holdEnd));
            SetCurve(serializedObject, ArmSideName, CreateConstantCurve(0f));
            SetCurve(
                serializedObject,
                ArmExtensionName,
                CreateLiftCurve(liftEnd, holdEnd));
            SetCurve(
                serializedObject,
                PenlightElevationName,
                CreateLiftCurve(liftEnd, holdEnd));
            SetCurve(serializedObject, PenlightSideName, CreateConstantCurve(0f));
            SetCurve(serializedObject, BodyLeanName, CreateLiftCurve(liftEnd, holdEnd));
        }

        private static AnimationCurve CreateSweepCurve()
            => CreateCurve(
                new Keyframe(0f, -1f),
                new Keyframe(0.25f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0f),
                new Keyframe(1f, -1f));

        private static AnimationCurve CreateLiftCurve(float liftEnd, float holdEnd)
            => CreateCurve(
                new Keyframe(0f, 0f),
                new Keyframe(liftEnd, 1f),
                new Keyframe(holdEnd, 1f),
                new Keyframe(1f, 0f));

        private static AnimationCurve CreateConstantCurve(float value)
            => AnimationCurve.Linear(0f, value, 1f, value);

        private static AnimationCurve CreateCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
            => serializedObject.FindProperty(propertyName).floatValue = value;

        private static void SetCurve(
            SerializedObject serializedObject,
            string propertyName,
            AnimationCurve curve)
            => serializedObject.FindProperty(propertyName).animationCurveValue = curve;
    }
}
