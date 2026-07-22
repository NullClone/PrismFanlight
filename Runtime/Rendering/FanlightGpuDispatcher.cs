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

        private static Vector3 ComputeWorldDirection(FanlightDirectionState direction)
        {
            var yaw = direction.WorldYawDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)).normalized;
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
            shader.SetVector(FanlightShaderIds.MotionAssetWeights, new Vector4(
                motion.GetAssetWeight(0),
                motion.GetAssetWeight(1),
                motion.GetAssetWeight(2),
                0f));
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
