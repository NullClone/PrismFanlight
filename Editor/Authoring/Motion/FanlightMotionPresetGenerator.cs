using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightMotionPresetGenerator
    {
        // Methods

        internal static void GenerateIdle(FanlightMotionAsset asset, float intensity)
        {
            intensity = ClampIntensity(intensity);
            GenerateTrajectory(
                asset,
                128,
                new Vector3(0f, 0.28f, 0.55f),
                new Vector3(0f, 0.75f, 0.5f),
                phase =>
                {
                    var angle = phase * Mathf.PI * 2f;
                    return ClampHand(new Vector3(
                        Mathf.Sin(angle) * 0.025f * intensity,
                        0.28f + Mathf.Sin(angle) * 0.025f * intensity,
                        0.55f + Mathf.Cos(angle) * 0.015f * intensity));
                },
                phase => new Vector3(0f, Mathf.Sin(phase * Mathf.PI * 2f) * 0.005f * intensity, 0f),
                phase => Quaternion.Euler(-1f + Mathf.Sin(phase * Mathf.PI * 2f) * 0.8f * intensity, 0f, 0f),
                0.15f,
                0.25f);
        }


        internal static void GenerateDrum(FanlightMotionAsset asset, in DrumParameters parameters, float intensity = 1f)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (!float.IsFinite(intensity)) throw new ArgumentOutOfRangeException(nameof(intensity));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();
            intensity = ClampIntensity(intensity);

            var samples = new FanlightMotionSample[128];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = EvaluateDrumSample((float)i / samples.Length, parameters, intensity);
            }

            var reference = new FanlightMotionSample(
                Vector3.zero,
                Quaternion.Euler(parameters.BaseBodyLean, 0f, 0f),
                SphericalPosition(parameters.BaseElevation, parameters.BaseSideAngle, parameters.BaseExtension),
                Quaternion.AngleAxis(parameters.BasePitch, Vector3.right));
            asset.SetSamples(reference, samples);
        }

        private static FanlightMotionSample EvaluateDrumSample(float phase, in DrumParameters p, float intensity)
        {
            phase = Mathf.Repeat(phase, 1f);
            var recovering = phase < p.RecoveryDuration;
            var progress = recovering
                ? phase / p.RecoveryDuration
                : (phase - p.RecoveryDuration) / (1f - p.RecoveryDuration);
            var lift = EvaluateDrumLift(phase, p.RecoveryDuration);
            var arc = 4f * progress * (1f - progress);
            arc = arc * arc * arc;

            var elevation = p.BaseElevation + (lift * 2f - 1f) * p.ElevationAmplitude * intensity;
            var extension = p.BaseExtension + lift * p.LiftExtension;
            extension += (recovering ? p.RecoveryExtensionArc : p.StrikeExtensionArc) * arc;
            var sideAngle = p.BaseSideAngle + (recovering ? p.RecoverySideArc : p.StrikeSideArc) * arc;
            var hand = SphericalPosition(elevation, sideAngle, extension);
            var wristPhase = Mathf.Repeat(phase - p.WristPhaseLag, 1f);
            var wristRecovering = wristPhase < p.RecoveryDuration;
            var wristProgress = wristRecovering
                ? wristPhase / p.RecoveryDuration
                : (wristPhase - p.RecoveryDuration) / (1f - p.RecoveryDuration);
            var wristLift = EvaluateDrumLift(wristPhase, p.RecoveryDuration);
            var wristArc = 4f * wristProgress * (1f - wristProgress);
            wristArc = wristArc * wristArc * wristArc;
            var pitch = p.BasePitch + (1f - wristLift) * p.PitchAmplitude
                                    + (wristRecovering ? p.RecoveryPitchArc : p.StrikePitchArc) * wristArc;
            pitch = p.PitchPivot + (pitch - p.PitchPivot) * intensity;
            var penlightRotation = Quaternion.AngleAxis(pitch, Vector3.right);

            var bodyLoad = 1f - EvaluateDrumLift(phase - p.BodyPhaseLag, p.RecoveryDuration);
            var bodyPosition = new Vector3(0f, p.BodySink * bodyLoad, p.BodyPush * bodyLoad) * intensity;
            var bodyRotation = Quaternion.Euler(p.BaseBodyLean + p.BodyLeanAmplitude * bodyLoad * intensity, 0f, 0f);
            return new FanlightMotionSample(bodyPosition, bodyRotation, hand, penlightRotation);
        }

        private static float EvaluateDrumLift(float phase, float recoveryDuration)
        {
            phase = Mathf.Repeat(phase, 1f);
            var recovering = phase < recoveryDuration;
            var progress = recovering
                ? phase / recoveryDuration
                : (phase - recoveryDuration) / (1f - recoveryDuration);
            var eased = progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
            return recovering ? eased : 1f - eased;
        }


        internal static void GenerateWiper(FanlightMotionAsset asset, in WiperParameters parameters, float intensity = 1f)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (!float.IsFinite(intensity)) throw new ArgumentOutOfRangeException(nameof(intensity));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();
            intensity = ClampIntensity(intensity);

            var samples = new FanlightMotionSample[128];
            for (var i = 0; i < samples.Length; i++)
            {
                var phase = (float)i / samples.Length;
                var hand = EvaluateWiperHand(phase, parameters, intensity);

                var wristSide = EvaluateWiperSide(phase - parameters.WristPhaseLag, parameters.TurnaroundEase);
                var wristCenter = 1f - wristSide * wristSide;
                var penlightDirection = Direction(
                    parameters.PenlightElevation
                    + parameters.PenlightCenterElevationArc * wristCenter * intensity,
                    parameters.SideBias
                    + parameters.PenlightSideAmplitude * wristSide * intensity);

                var bodySide = EvaluateWiperSide(phase - parameters.BodyPhaseLag, parameters.TurnaroundEase);
                var bodyTurn = bodySide * bodySide;
                var bodyPosition = new Vector3(
                    bodySide * parameters.BodySideShift * intensity,
                    -bodyTurn * parameters.BodyVerticalBounce * intensity,
                    0f);
                var bodyRotation = Quaternion.Euler(
                    parameters.BaseBodyLean,
                    bodySide * parameters.BodyYawAmplitude * intensity,
                    -bodySide * parameters.BodyRollAmplitude * intensity);

                samples[i] = new FanlightMotionSample(
                    bodyPosition,
                    bodyRotation,
                    hand,
                    CreatePenlightRotation(penlightDirection, Vector3.forward));
            }

            var referenceDirection = Direction(parameters.PenlightElevation, parameters.SideBias);
            var reference = new FanlightMotionSample(
                Vector3.zero,
                Quaternion.Euler(parameters.BaseBodyLean, 0f, 0f),
                SphericalPosition(parameters.BaseElevation, parameters.SideBias, parameters.BaseExtension),
                CreatePenlightRotation(referenceDirection, Vector3.forward));
            asset.SetSamples(reference, samples);
        }

        private static Vector3 EvaluateWiperHand(float phase, in WiperParameters parameters, float intensity)
        {
            var side = EvaluateWiperSide(phase, parameters.TurnaroundEase);
            var center = 1f - side * side;
            return SphericalPosition(
                parameters.BaseElevation + parameters.CenterElevationArc * center * intensity,
                parameters.SideBias + parameters.SweepAngle * side * intensity,
                parameters.BaseExtension + parameters.CenterExtensionArc * center * intensity);
        }

        private static float EvaluateWiperSide(float phase, float turnaroundEase)
        {
            var side = Mathf.Sin(Mathf.Repeat(phase, 1f) * Mathf.PI * 2f);
            var magnitude = Mathf.Abs(side);
            var exponent = Mathf.Lerp(1f, 4f, turnaroundEase);
            return Mathf.Sign(side) * (1f - Mathf.Pow(1f - magnitude, exponent));
        }


        internal static void GenerateSasage(FanlightMotionAsset asset, in SasageParameters parameters, float intensity = 1f)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (!float.IsFinite(intensity)) throw new ArgumentOutOfRangeException(nameof(intensity));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();
            intensity = ClampIntensity(intensity);

            var samples = new FanlightMotionSample[128];
            var phaseStep = 0.5f / samples.Length;
            for (var i = 0; i < samples.Length; i++)
            {
                var phase = (float)i / samples.Length;
                var angle = phase * Mathf.PI * 2f;
                var hand = EvaluateSasageHand(phase, parameters, intensity);
                var wristPhase = phase - parameters.WristPhaseLag;
                var wristHand = EvaluateSasageHand(wristPhase, parameters, intensity);
                var tangent = SafeNormalize(
                    EvaluateSasageHand(wristPhase + phaseStep, parameters, intensity)
                    - EvaluateSasageHand(wristPhase - phaseStep, parameters, intensity),
                    Vector3.up);
                var radial = SafeNormalize(wristHand, new Vector3(0f, 0.96f, 0.28f));
                var penlightDirection = SafeNormalize(
                    Vector3.Lerp(radial, tangent, parameters.PenlightTangentWeight)
                    + Vector3.up * parameters.PenlightUpwardBias,
                    radial);
                var bodyPosition = new Vector3(
                    0f,
                    (1f - Mathf.Cos(angle)) * parameters.BodyRiseLift * intensity,
                    0f);
                var bodyRotation = Quaternion.Euler(
                    parameters.BaseBodyLean - Mathf.Sin(angle) * parameters.BodyLeanAmplitude * intensity,
                    0f,
                    -Mathf.Sin(angle) * parameters.BodyRollAmplitude * intensity);

                samples[i] = new FanlightMotionSample(
                    bodyPosition,
                    bodyRotation,
                    hand,
                    CreatePenlightRotation(penlightDirection, Vector3.right));
            }

            var reference = new FanlightMotionSample(
                Vector3.zero,
                Quaternion.Euler(-1f, 0f, 0f),
                ClampHand(new Vector3(0f, 0.62f, 0.42f)),
                CreatePenlightRotation(new Vector3(0f, 0.96f, 0.28f), Vector3.right));
            asset.SetSamples(reference, samples);
        }

        private static Vector3 EvaluateSasageHand(float phase, in SasageParameters parameters, float intensity)
        {
            var angle = phase * Mathf.PI * 2f;
            return ClampHand(new Vector3(
                Mathf.Sin(angle) * parameters.SideAmplitude * intensity,
                parameters.BaseHandHeight - Mathf.Cos(angle) * parameters.VerticalAmplitude * intensity,
                parameters.BaseHandDepth + Mathf.Sin(angle) * parameters.DepthAmplitude * intensity));
        }


        internal static void GenerateCheer(FanlightMotionAsset asset, float intensity)
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
        {
            return Direction(elevationDegrees, sideDegrees) * Mathf.Clamp01(extension);
        }

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

        private static Vector3 ClampHand(Vector3 hand) => Vector3.ClampMagnitude(hand, 0.98f);

        private static Vector3 SafeNormalize(Vector3 value, Vector3 fallback) => value.sqrMagnitude > 0.000001f ? value.normalized : fallback;

        private static float ClampIntensity(float intensity) => Mathf.Clamp(intensity, 0.65f, 1.25f);
    }
}
