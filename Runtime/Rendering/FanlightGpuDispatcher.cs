using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuDispatcher
    {
        // Fields

        private const int InstanceThreadGroupSize = 128;
        private const int BlockThreadGroupSize = 64;

        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustumPlanes = new Vector4[6];
        private readonly Vector4[] _colorSourceModes = new Vector4[3];
        private readonly Vector4[] _colorSourcePalette = new Vector4[18];
        private readonly Vector4[] _colorSourceA = new Vector4[3];
        private readonly Vector4[] _colorSourceB = new Vector4[3];
        private readonly Vector4[] _colorSourceGeometry = new Vector4[3];
        private readonly Vector4[] _colorSourceParameters = new Vector4[3];
        private readonly Vector4[] _maskSourceModes = new Vector4[3];
        private readonly Vector4[] _maskSourceTiming = new Vector4[3];
        private readonly Vector4[] _maskSourceEnvelope = new Vector4[3];
        private readonly Vector4[] _maskSourceGeometry = new Vector4[3];


        // Methods

        internal void DispatchVisibility(
            ComputeShader shader,
            FanlightGpuKernels kernels,
            FanlightGpuBuffers buffers,
            in FanlightGpuDispatchContext context)
        {
            SetCommonParams(shader, context, buffers, true);

            shader.SetBuffer(kernels.ClearIndirectArgs, FanlightShaderIds.PenlightArgs, buffers.PenlightArgsBuffer);
            shader.SetBuffer(kernels.ClearIndirectArgs, FanlightShaderIds.AudienceArgs, buffers.AudienceArgsBuffer);
            shader.Dispatch(kernels.ClearIndirectArgs, 1, 1, 1);

            shader.SetBuffer(kernels.CullBlocks, FanlightShaderIds.Blocks, buffers.BlockBuffer);
            shader.SetBuffer(kernels.CullBlocks, FanlightShaderIds.BlockVisibility, buffers.BlockVisibilityBuffer);
            shader.Dispatch(kernels.CullBlocks, Mathf.CeilToInt((float)buffers.BlockCount / BlockThreadGroupSize), 1, 1);

            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.BlockVisibility, buffers.BlockVisibilityBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.PenlightVisibleIndices, buffers.PenlightVisibleIndexBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.PenlightVariantAssignments, buffers.PenlightVariantAssignmentBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.PenlightVariantOffsets, buffers.PenlightVariantOffsetBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.AudienceVisibleIndices, buffers.AudienceVisibleIndexBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.AudienceSlots, buffers.AudienceSlotBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.PenlightArgs, buffers.PenlightArgsBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.AudienceArgs, buffers.AudienceArgsBuffer);
            shader.Dispatch(kernels.BuildVisibleInstances, Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize), 1, 1);
        }

        internal void DispatchAnimation(
            ComputeShader shader,
            FanlightGpuKernels kernels,
            FanlightGpuBuffers buffers,
            in FanlightGpuDispatchContext context,
            bool visibleOnly)
        {
            SetCommonParams(shader, context, buffers, false);
            SetAudienceParams(shader, context);
            shader.SetVector(FanlightShaderIds.MotionReferenceArm, buffers.MotionReferenceArm);
            shader.SetVector(FanlightShaderIds.MotionReferencePenlight, buffers.MotionReferencePenlight);

            var kernel = buffers.HasAudience
                ? visibleOnly ? kernels.GenerateVisibleFrameData : kernels.GenerateAllFrameData
                : visibleOnly
                    ? kernels.GenerateVisibleAnimation
                    : kernels.GenerateAllAnimation;

            shader.SetBuffer(kernel, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.Randoms, buffers.RandomBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.MotionSamples, buffers.MotionSampleBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.PenlightVisibleIndices, buffers.PenlightVisibleIndexBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.PenlightVariantAssignments, buffers.PenlightVariantAssignmentBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.PenlightVariantOffsets, buffers.PenlightVariantOffsetBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.AudienceVisibleIndices, buffers.AudienceVisibleIndexBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.AudienceSlots, buffers.AudienceSlotBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.PenlightArgs, buffers.PenlightArgsBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.AudienceArgs, buffers.AudienceArgsBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.Matrices, buffers.MatrixBuffer);

            if (buffers.HasAudience)
            {
                shader.SetBuffer(kernel, FanlightShaderIds.AudienceParts, buffers.AudiencePartBuffer);
            }

            shader.Dispatch(kernel, Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize), 1, 1);
        }

        internal void DispatchColor(
            ComputeShader shader,
            FanlightGpuKernels kernels,
            FanlightGpuBuffers buffers,
            in FanlightGpuDispatchContext context)
        {
            var color = context.Sample.State.Color.Validated();
            buffers.UpdateRuntimeBlockPaletteData(color, context.Layout);

            for (var sourceIndex = 0; sourceIndex < 3; sourceIndex++)
            {
                var source = color.GetSource(sourceIndex);
                var weight = color.GetSourceWeight(sourceIndex);
                _colorSourceModes[sourceIndex] = new Vector4(
                    (int)source.Mode,
                    weight,
                    sourceIndex * buffers.BlockCount,
                    0f);
                _colorSourceA[sourceIndex] = Vector4.zero;
                _colorSourceB[sourceIndex] = Vector4.zero;
                _colorSourceGeometry[sourceIndex] = Vector4.zero;
                _colorSourceParameters[sourceIndex] = Vector4.zero;
                for (var slotIndex = 0; slotIndex < 6; slotIndex++)
                {
                    _colorSourcePalette[sourceIndex * 6 + slotIndex] = Vector4.zero;
                }

                if (weight <= 0f) continue;

                if (source.Mode == FanlightColorMode.LinearGradient)
                {
                    _colorSourceA[sourceIndex] = ToLinearChroma(source.ColorA);
                    _colorSourceB[sourceIndex] = ToLinearChroma(source.ColorB);
                    _colorSourceGeometry[sourceIndex] = new Vector4(
                        source.Origin.x,
                        source.Origin.y,
                        source.Direction.x,
                        source.Direction.y);
                    _colorSourceParameters[sourceIndex] = new Vector4(source.Width, source.Offset, 0f, 0f);
                    continue;
                }

                for (var slotIndex = 0; slotIndex < 6; slotIndex++)
                {
                    _colorSourcePalette[sourceIndex * 6 + slotIndex] =
                        ToLinearChroma(source.GetPaletteSlot(slotIndex));
                }
            }

            shader.SetInt(FanlightShaderIds.InstanceCount, buffers.SeatCount);
            shader.SetVector(FanlightShaderIds.BlockCount, new Vector4(
                context.Layout.BlockCount2D.x,
                context.Layout.BlockCount2D.y,
                0f,
                0f));
            shader.SetVectorArray(FanlightShaderIds.ColorSourceModes, _colorSourceModes);
            shader.SetVectorArray(FanlightShaderIds.ColorSourcePalette, _colorSourcePalette);
            shader.SetVectorArray(FanlightShaderIds.ColorSourceA, _colorSourceA);
            shader.SetVectorArray(FanlightShaderIds.ColorSourceB, _colorSourceB);
            shader.SetVectorArray(FanlightShaderIds.ColorSourceGeometry, _colorSourceGeometry);
            shader.SetVectorArray(FanlightShaderIds.ColorSourceParameters, _colorSourceParameters);
            shader.SetBuffer(kernels.ResolveSeatChroma, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernels.ResolveSeatChroma, FanlightShaderIds.StableAssignments, buffers.StableAssignmentBuffer);
            shader.SetBuffer(kernels.ResolveSeatChroma, FanlightShaderIds.RuntimeBlockPalettes, buffers.RuntimeBlockPaletteBuffer);
            shader.SetBuffer(kernels.ResolveSeatChroma, FanlightShaderIds.ResolvedChroma, buffers.ResolvedChromaBuffer);
            shader.Dispatch(
                kernels.ResolveSeatChroma,
                Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize),
                1,
                1);
        }

        internal void DispatchMask(
            ComputeShader shader,
            FanlightGpuKernels kernels,
            FanlightGpuBuffers buffers,
            in FanlightGpuDispatchContext context)
        {
            var intensity = context.Sample.State.Intensity.Validated();

            for (var sourceIndex = 0; sourceIndex < 3; sourceIndex++)
            {
                var mask = intensity.GetMask(sourceIndex);
                var weight = intensity.GetMaskWeight(sourceIndex);
                _maskSourceModes[sourceIndex] = new Vector4(
                    (int)mask.Mode,
                    weight,
                    0f,
                    0f);
                _maskSourceTiming[sourceIndex] = Vector4.zero;
                _maskSourceEnvelope[sourceIndex] = Vector4.zero;
                _maskSourceGeometry[sourceIndex] = Vector4.zero;

                if (weight <= 0f || mask.Mode == FanlightIntensityMaskMode.None) continue;

                _maskSourceTiming[sourceIndex] = new Vector4(
                    mask.BeatsPerCycle,
                    mask.PhaseOffsetBeats,
                    0f,
                    0f);
                _maskSourceEnvelope[sourceIndex] = new Vector4(
                    mask.MinimumIntensityRatio,
                    mask.AttackRatio,
                    mask.HoldRatio,
                    mask.ReleaseRatio);

                if (mask.Mode == FanlightIntensityMaskMode.TravelingWave)
                {
                    _maskSourceTiming[sourceIndex].z = mask.Wavelength;
                    _maskSourceGeometry[sourceIndex] = new Vector4(
                        mask.Origin.x,
                        mask.Origin.y,
                        mask.Direction.x,
                        mask.Direction.y);
                }
            }

            shader.SetInt(FanlightShaderIds.InstanceCount, buffers.SeatCount);
            shader.SetFloat(
                FanlightShaderIds.MaskCompletedBeat,
                (float)context.Sample.MusicalPosition.Beat);
            shader.SetVectorArray(FanlightShaderIds.MaskSourceModes, _maskSourceModes);
            shader.SetVectorArray(FanlightShaderIds.MaskSourceTiming, _maskSourceTiming);
            shader.SetVectorArray(FanlightShaderIds.MaskSourceEnvelope, _maskSourceEnvelope);
            shader.SetVectorArray(FanlightShaderIds.MaskSourceGeometry, _maskSourceGeometry);
            shader.SetBuffer(kernels.ResolveSeatMask, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernels.ResolveSeatMask, FanlightShaderIds.ResolvedMask, buffers.ResolvedMaskBuffer);
            shader.Dispatch(
                kernels.ResolveSeatMask,
                Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize),
                1,
                1);
        }

        private static Vector3 ComputeWorldDirection(FanlightDirectionState direction)
        {
            var yaw = direction.WorldYawDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)).normalized;
        }

        private static Color ToLinearChroma(Color value)
        {
            var linear = QualitySettings.activeColorSpace == ColorSpace.Gamma ? value.linear : value;
            linear.a = 1f;
            return linear;
        }

        private static void SetAudienceParams(ComputeShader shader, in FanlightGpuDispatchContext context)
        {
            var state = context.Sample.State;
            var audience = state.AudienceBody;
            var realism = state.Intent.Realism;
            var energy = state.Intent.Energy;
            var worldScale = FanlightGeometryBuilder.GetMaxScale(context.Frame.LocalToWorld);

            shader.SetVector(FanlightShaderIds.AudienceShape, new Vector4(
                audience.Height,
                audience.HeightVariation * realism,
                audience.ShoulderHeightRatio,
                audience.Width * 0.5f));
            shader.SetVector(FanlightShaderIds.AudienceArm, new Vector4(
                audience.ArmWidth * 0.5f,
                audience.ShoulderSideOffset,
                audience.HeadSize * 0.5f,
                audience.ArmLengthLimit));
            shader.SetVector(FanlightShaderIds.AudienceUpperBody, new Vector4(
                audience.UpperBodyLean * realism,
                audience.UpperBodyLeanMaximumRadians,
                worldScale,
                0f));
            shader.SetVector(FanlightShaderIds.AudienceMotionBody, new Vector4(
                audience.Bounce * realism * energy,
                audience.Sway * realism * energy,
                audience.MotionSpeed,
                audience.LeanMotion * realism * energy));
        }

        private void SetCommonParams(
            ComputeShader shader,
            in FanlightGpuDispatchContext context,
            FanlightGpuBuffers buffers,
            bool includeVisibilityParams)
        {
            var layout = context.Layout;
            var sample = context.Sample;
            var state = sample.State;
            var intent = state.Intent;
            var motion = state.Motion;
            var variation = state.Variation;
            var noise = state.Noise;
            var rest = state.Rest;
            var direction = state.Direction;
            var musical = sample.MusicalPosition;
            var realism = intent.Realism;
            var asynchrony = 1f - intent.Synchronization;

            shader.SetInt(FanlightShaderIds.InstanceCount, buffers.SeatCount);
            shader.SetInt(FanlightShaderIds.PenlightVariantCount, buffers.PenlightVariantCount);
            shader.SetVector(FanlightShaderIds.PenlightVariantGripPivotYs, buffers.PenlightVariantGripPivotYs);
            shader.SetInt(FanlightShaderIds.BlockCountValue, buffers.BlockCount);
            shader.SetMatrix(FanlightShaderIds.LocalToWorld, context.Frame.LocalToWorld);
            shader.SetMatrix(FanlightShaderIds.WorldToLocal, context.WorldToLocal);
            shader.SetFloat(FanlightShaderIds.Time, (float)sample.AnimationSampleSeconds);
            shader.SetVector(FanlightShaderIds.Beat, new Vector4(
                (float)musical.Seconds,
                (float)musical.Beat,
                (float)musical.BeatPhase,
                (float)musical.BarPhase));
            shader.SetVector(FanlightShaderIds.Tempo, new Vector4(
                1f,
                (float)musical.Bpm,
                musical.BeatsPerBar,
                0f));

            shader.SetVector(FanlightShaderIds.SeatPitch, new Vector4(layout.SeatPitch.x, layout.SeatPitch.y, 0f, 0f));
            shader.SetVector(FanlightShaderIds.BlockCount, new Vector4(layout.BlockCount2D.x, layout.BlockCount2D.y, 0f, 0f));

            if (includeVisibilityParams)
            {
                shader.SetFloat(FanlightShaderIds.CullingScale, FanlightGeometryBuilder.GetMaxScale(context.Frame.LocalToWorld));
                shader.SetInt(FanlightShaderIds.EnableCulling, context.Camera.CullingEnabled ? 1 : 0);
                shader.SetInt(FanlightShaderIds.EnableAudienceLod, 0);
                shader.SetVector(FanlightShaderIds.AudienceLod, Vector4.zero);
                var cameraPosition = context.Camera.WorldPosition;
                shader.SetVector(FanlightShaderIds.LodCameraPos, new Vector4(cameraPosition.x, cameraPosition.y, cameraPosition.z, 1f));
                SetFrustumPlanes(shader, context.Camera.CullingEnabled ? context.Camera.Camera : null, context.WorldBounds);
            }

            var worldDirection = ComputeWorldDirection(direction);
            shader.SetVector(FanlightShaderIds.MotionTiming, new Vector4(intent.Reach, asynchrony, noise.PhaseAmount * realism, noise.PhaseSpeed));
            shader.SetVector(FanlightShaderIds.MotionCycle, new Vector4(
                motion.BeatsPerCycle,
                motion.PhaseOffsetBeats,
                motion.WristDelayRatio,
                motion.Variation));
            shader.SetVector(FanlightShaderIds.MotionParameters, new Vector4(
                motion.MotionAmount,
                motion.HeightBias,
                motion.SideScale,
                motion.ForwardScale));
            shader.SetInt(FanlightShaderIds.SwingMode, (int)direction.Mode);
            shader.SetVector(FanlightShaderIds.SwingAxis, new Vector4(worldDirection.x, worldDirection.y, worldDirection.z, variation.DirectionSpread * realism));
            var target = context.Frame.SwingTargetWorldPosition;
            shader.SetVector(FanlightShaderIds.SwingTargetPos, new Vector4(target.x, target.y, target.z, direction.AimStrength));
            shader.SetVector(FanlightShaderIds.MotionVariation, new Vector4(
                variation.SeatPosition * realism,
                variation.BodyHeight * realism,
                variation.ArmLength * realism,
                variation.Angle * realism));
            shader.SetVector(FanlightShaderIds.MotionNoise, new Vector4(
                noise.AxisAmount * realism,
                noise.AxisSpeed,
                noise.Octaves,
                noise.Persistence));
            shader.SetVector(FanlightShaderIds.MotionHuman, new Vector4(
                intent.Energy * 2f,
                variation.EnergyResponse * realism,
                variation.ReactionDelaySeconds * asynchrony * realism,
                variation.Speed));
            shader.SetVector(FanlightShaderIds.MotionRest, new Vector4(
                rest.Probability * realism,
                rest.MotionLevel,
                1f - intent.Participation,
                0f));
            shader.SetVector(FanlightShaderIds.MotionRestTiming, new Vector4(
                rest.CycleSeconds,
                rest.DurationSeconds,
                rest.FadeSeconds,
                rest.PhaseRandomness * realism));
            shader.SetVector(FanlightShaderIds.MotionBeatSpread, new Vector4(
                variation.BeatReactionDelaySeconds * asynchrony * realism,
                variation.BeatJitter * asynchrony * realism,
                variation.BlockDelayXBeats * asynchrony * realism,
                variation.BlockDelayYBeats * asynchrony * realism));
            shader.SetFloat(FanlightShaderIds.HandPositionSpread, variation.HandZone * realism);
        }

        private void SetFrustumPlanes(ComputeShader shader, Camera cullingCamera, Bounds worldBounds)
        {
            if (!cullingCamera)
            {
                SetAlwaysVisiblePlanes(worldBounds);
            }
            else
            {
                GeometryUtility.CalculateFrustumPlanes(cullingCamera, _planes);

                for (var i = 0; i < _planes.Length; i++)
                {
                    var plane = _planes[i];
                    var normal = plane.normal;
                    _frustumPlanes[i] = new Vector4(normal.x, normal.y, normal.z, plane.distance);
                }
            }

            shader.SetVectorArray(FanlightShaderIds.FrustumPlanes, _frustumPlanes);
        }

        private void SetAlwaysVisiblePlanes(Bounds bounds)
        {
            var center = bounds.center;
            var radius = bounds.extents.magnitude + 1f;

            _frustumPlanes[0] = new Vector4(1, 0, 0, radius - center.x);
            _frustumPlanes[1] = new Vector4(-1, 0, 0, radius + center.x);
            _frustumPlanes[2] = new Vector4(0, 1, 0, radius - center.y);
            _frustumPlanes[3] = new Vector4(0, -1, 0, radius + center.y);
            _frustumPlanes[4] = new Vector4(0, 0, 1, radius - center.z);
            _frustumPlanes[5] = new Vector4(0, 0, -1, radius + center.z);
        }
    }
}
