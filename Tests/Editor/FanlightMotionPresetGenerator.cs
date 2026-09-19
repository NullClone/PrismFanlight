using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightMotionPresetGenerator
    {
        // Methods

        internal static void GenerateDrum(FanlightMotionAsset asset)
        {
            asset.ResetToDrum();
        }

        internal static void GenerateWiper(
            FanlightMotionAsset asset,
            float sweepAngle,
            float armElevation,
            float armExtension,
            float penlightElevation,
            float intensity)
        {
            intensity = ClampIntensity(intensity);
            sweepAngle = Mathf.Clamp(sweepAngle, 20f, 75f) * intensity;
            armElevation = Mathf.Clamp(armElevation, 20f, 70f);
            armExtension = Mathf.Clamp(armExtension, 0.5f, 1f);
            penlightElevation = Mathf.Clamp(penlightElevation, 30f, 90f);
            var referenceHand = SphericalPosition(armElevation, 0f, armExtension);
            GenerateTrajectory(
                asset,
                128,
                referenceHand,
                Direction(penlightElevation, 0f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return SphericalPosition(
                        armElevation + Mathf.Cos(angle * 2f) * 5f * intensity,
                        Mathf.Sin(angle) * sweepAngle,
                        armExtension - (1f - Mathf.Cos(angle * 2f)) * 0.025f);
                },
                phase => new Vector3(Mathf.Sin(phase * Mathf.PI * 2f) * 0.018f * intensity, 0f, 0f),
                phase => Quaternion.Euler(
                    -1f,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 3f * intensity),
                0.28f,
                0.25f);
        }

        internal static void GenerateSasage(
            FanlightMotionAsset asset,
            float lowElevation,
            float highElevation,
            float lowExtension,
            float highExtension,
            float holdRatio,
            float intensity)
        {
            intensity = ClampIntensity(intensity);
            lowElevation = Mathf.Clamp(lowElevation, -10f, 45f);
            highElevation = Mathf.Clamp(Mathf.Max(lowElevation, highElevation), 30f, 90f);
            lowExtension = Mathf.Clamp(lowExtension, 0.4f, 0.9f);
            highExtension = Mathf.Clamp(Mathf.Max(lowExtension, highExtension), 0.7f, 1f);
            holdRatio = Mathf.Clamp(holdRatio, 0.1f, 0.55f);
            var referenceHand = SphericalPosition(lowElevation, 0f, lowExtension);
            GenerateTrajectory(
                asset,
                128,
                referenceHand,
                Direction(lowElevation + 28f, 0f),
                phase =>
                {
                    var lift = PeriodicLift(phase, holdRatio);
                    var side = Mathf.Sin(phase * Mathf.PI * 2f) * 0.04f * intensity;
                    return SphericalPosition(
                        Mathf.Lerp(lowElevation, highElevation, lift),
                        side * Mathf.Rad2Deg,
                        Mathf.Lerp(lowExtension, highExtension, lift));
                },
                phase => new Vector3(0f, PeriodicLift(phase, holdRatio) * 0.025f * intensity, 0f),
                phase => Quaternion.Euler(-1f - PeriodicLift(phase, holdRatio) * 4f * intensity, 0f, 0f),
                0.18f,
                0.35f);
        }

        internal static void GeneratePowerPump(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.62f, 0.42f),
                new Vector3(0f, 0.96f, 0.28f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.12f * intensity,
                        0.37f - Mathf.Cos(angle) * 0.49f * intensity,
                        0.34f + Mathf.Sin(angle) * 0.18f * intensity));
                },
                phase => new Vector3(0f, (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.018f * intensity, 0f),
                phase => Quaternion.Euler(
                    -2f - Mathf.Sin(phase * Mathf.PI * 2f) * 4f * intensity,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 2.5f * intensity),
                0.32f,
                0.38f);
        }

        internal static void GenerateDoublePump(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.58f, 0.40f),
                new Vector3(0f, 0.95f, 0.31f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 4f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.08f * intensity,
                        0.38f - Mathf.Cos(angle) * 0.43f * intensity,
                        0.36f + Mathf.Sin(angle) * 0.14f * intensity));
                },
                phase => new Vector3(0f, (1f - Mathf.Cos(phase * Mathf.PI * 4f)) * 0.012f * intensity, 0f),
                phase => Quaternion.Euler(-2f - Mathf.Sin(phase * Mathf.PI * 4f) * 3f * intensity, 0f, 0f),
                0.3f,
                0.4f);
        }

        internal static void GenerateDiagonalPump(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0.24f, 0.58f, 0.38f),
                new Vector3(0.3f, 0.9f, 0.3f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    var drive = -Mathf.Cos(angle);
                    var orbit = Mathf.Sin(angle);
                    return ClampHand(new Vector3(
                        0.08f + drive * 0.36f * intensity + orbit * 0.08f,
                        0.40f + drive * 0.40f * intensity - orbit * 0.06f,
                        0.34f + orbit * 0.14f * intensity));
                },
                phase => new Vector3(
                    -Mathf.Cos(phase * Mathf.PI * 2f) * 0.018f * intensity,
                    0f,
                    0f),
                phase => Quaternion.Euler(
                    -2f - Mathf.Sin(phase * Mathf.PI * 2f) * 2f,
                    0f,
                    -Mathf.Cos(phase * Mathf.PI * 2f) * 5f * intensity),
                0.34f,
                0.36f);
        }

        internal static void GenerateForwardThrust(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.38f, 0.55f),
                new Vector3(0f, 0.62f, 0.78f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.13f * intensity,
                        0.40f + Mathf.Sin(angle) * 0.17f * intensity,
                        0.40f - Mathf.Cos(angle) * 0.36f * intensity));
                },
                phase => new Vector3(
                    Mathf.Sin(phase * Mathf.PI * 2f) * 0.01f,
                    0f,
                    (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.018f * intensity),
                phase => Quaternion.Euler(
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 5f * intensity,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 2f),
                0.22f,
                0.18f);
        }

        internal static void GenerateOverheadSwing(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.82f, 0.24f),
                new Vector3(0f, 0.98f, 0.18f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.58f * intensity,
                        0.72f + Mathf.Cos(angle * 2f) * 0.12f * intensity,
                        0.25f + Mathf.Cos(angle) * 0.08f));
                },
                phase => new Vector3(Mathf.Sin(phase * Mathf.PI * 2f) * 0.025f * intensity, 0f, 0f),
                phase => Quaternion.Euler(
                    -2f,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 6f * intensity),
                0.42f,
                0.32f);
        }

        internal static void GenerateCircle(FanlightMotionAsset asset, float intensity, bool clockwise)
        {
            intensity = ClampIntensity(intensity);
            var direction = clockwise ? -1f : 1f;
            GenerateTrajectory(
                asset,
                256,
                new Vector3(0f, 0.60f, 0.28f),
                new Vector3(0f, 0.95f, 0.22f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f * direction;
                    return ClampHand(new Vector3(
                        Mathf.Cos(angle) * 0.48f * intensity,
                        0.54f + Mathf.Sin(angle) * 0.36f * intensity,
                        0.25f + Mathf.Cos(angle + Mathf.PI * 0.5f) * 0.10f * intensity));
                },
                phase => new Vector3(
                    Mathf.Cos(phase * Mathf.PI * 2f * direction) * 0.02f * intensity,
                    0.008f * (1f - Mathf.Cos(phase * Mathf.PI * 4f)),
                    0f),
                phase => Quaternion.Euler(
                    -2f,
                    0f,
                    -Mathf.Cos(phase * Mathf.PI * 2f * direction) * 5f * intensity),
                0.68f,
                0.08f);
        }

        internal static void GenerateFigureEight(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                256,
                new Vector3(0f, 0.58f, 0.26f),
                new Vector3(0f, 0.95f, 0.22f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.50f * intensity,
                        0.57f + Mathf.Sin(angle * 2f) * 0.27f * intensity,
                        0.25f + Mathf.Cos(angle) * 0.10f * intensity));
                },
                phase => new Vector3(Mathf.Sin(phase * Mathf.PI * 2f) * 0.018f * intensity, 0f, 0f),
                phase => Quaternion.Euler(
                    -2f,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 5f * intensity),
                0.62f,
                0.12f);
        }

        internal static void GenerateGrooveBounce(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.45f, 0.52f),
                new Vector3(0f, 0.88f, 0.48f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.18f * intensity,
                        0.45f + Mathf.Sin(angle * 2f) * 0.20f * intensity,
                        0.50f + Mathf.Cos(angle) * 0.10f * intensity));
                },
                phase => new Vector3(
                    Mathf.Sin(phase * Mathf.PI * 2f) * 0.018f * intensity,
                    (1f - Mathf.Cos(phase * Mathf.PI * 4f)) * 0.012f * intensity,
                    0f),
                phase => Quaternion.Euler(
                    -1f - Mathf.Sin(phase * Mathf.PI * 4f) * 3f * intensity,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 4f * intensity),
                0.3f,
                0.28f);
        }

        internal static void GenerateRaisedSway(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.78f, 0.30f),
                new Vector3(0f, 0.98f, 0.18f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.32f * intensity,
                        0.76f + Mathf.Cos(angle * 2f) * 0.07f * intensity,
                        0.30f + Mathf.Cos(angle) * 0.08f * intensity));
                },
                phase => new Vector3(Mathf.Sin(phase * Mathf.PI * 2f) * 0.018f * intensity, 0f, 0f),
                phase => Quaternion.Euler(
                    -2f,
                    0f,
                    -Mathf.Sin(phase * Mathf.PI * 2f) * 4f * intensity),
                0.34f,
                0.32f);
        }

        private static void GenerateTrajectory(
            FanlightMotionAsset asset,
            int sampleCount,
            Vector3 referenceHand,
            Vector3 referencePenlightDirection,
            Func<float, Vector3> handPath,
            Func<float, Vector3> bodyPosition,
            Func<float, Quaternion> bodyRotation,
            float tangentWeight,
            float upwardBias)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            var samples = new FanlightMotionSample[sampleCount];
            var phaseStep = 0.5f / sampleCount;
            for (var i = 0; i < sampleCount; i++)
            {
                var phase = (float)i / sampleCount;
                var hand = handPath(phase);
                var tangent = SafeNormalize(
                    handPath(phase + phaseStep) - handPath(phase - phaseStep),
                    Vector3.up);
                var radial = SafeNormalize(hand, referencePenlightDirection);
                var penlightDirection = SafeNormalize(
                    Vector3.Lerp(radial, tangent, tangentWeight) + Vector3.up * upwardBias,
                    radial);
                samples[i] = new FanlightMotionSample(
                    bodyPosition(phase),
                    bodyRotation(phase),
                    hand,
                    CreatePenlightRotation(penlightDirection, tangent));
            }

            var reference = new FanlightMotionSample(
                Vector3.zero,
                Quaternion.Euler(-1f, 0f, 0f),
                ClampHand(referenceHand),
                CreatePenlightRotation(referencePenlightDirection, Vector3.forward));
            asset.SetSamples(reference, samples);
        }

        private static Quaternion CreatePenlightRotation(Vector3 up, Vector3 motionTangent)
        {
            up = SafeNormalize(up, Vector3.up);
            var forward = motionTangent - up * Vector3.Dot(motionTangent, up);
            if (forward.sqrMagnitude <= 0.000001f)
            {
                forward = Vector3.Cross(up, Mathf.Abs(up.x) < 0.8f ? Vector3.right : Vector3.forward);
            }

            return Quaternion.LookRotation(SafeNormalize(forward, Vector3.forward), up);
        }

        private static Vector3 SphericalPosition(float elevationDegrees, float sideDegrees, float extension)
            => Direction(elevationDegrees, sideDegrees) * Mathf.Clamp01(extension);

        private static Vector3 Direction(float elevationDegrees, float sideDegrees)
        {
            var elevation = elevationDegrees * Mathf.Deg2Rad;
            var side = sideDegrees * Mathf.Deg2Rad;
            var cosElevation = Mathf.Cos(elevation);
            return SafeNormalize(
                new Vector3(
                    Mathf.Sin(side) * cosElevation,
                    Mathf.Sin(elevation),
                    Mathf.Cos(side) * cosElevation),
                Vector3.up);
        }

        private static Vector3 ClampHand(Vector3 hand)
            => Vector3.ClampMagnitude(hand, 0.98f);

        private static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
            => value.sqrMagnitude > 0.000001f ? value.normalized : fallback;

        private static float PeriodicLift(float phase, float holdRatio)
        {
            phase = Mathf.Repeat(phase, 1f);
            var travel = Mathf.Max(0.08f, (1f - holdRatio) * 0.5f);
            if (phase < travel) return Smooth01(phase / travel);
            if (phase < travel + holdRatio) return 1f;
            return 1f - Smooth01((phase - travel - holdRatio) / travel);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float ClampIntensity(float intensity) => Mathf.Clamp(intensity, 0.65f, 1.25f);
    }
}
