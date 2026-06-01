using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuDispatcher
    {
        public const int InstanceThreadGroupSize = 128;
        public const int BlockThreadGroupSize = 64;

        private readonly Vector4[] _paletteColors = new Vector4[FanlightColorSettings.MaxPaletteColors];
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustumPlanes = new Vector4[6];

        private static Vector3 ComputeWorldDirection(FanlightMotionSettings motion)
        {
            var yaw = motion.direction.swingYaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)).normalized;
        }


        public void DispatchVisibility(ComputeShader shader, FanlightGpuKernels kernels, FanlightGpuBuffers buffers, FanlightGpuDispatchContext context)
        {
            SetCommonParams(shader, context, buffers, true);

            shader.SetBuffer(kernels.ClearIndirectArgs, FanlightShaderIds.DrawArgs, buffers.ArgsBuffer);
            shader.Dispatch(kernels.ClearIndirectArgs, 1, 1, 1);

            shader.SetBuffer(kernels.CullBlocks, FanlightShaderIds.Blocks, buffers.BlockBuffer);
            shader.SetBuffer(kernels.CullBlocks, FanlightShaderIds.BlockVisibility, buffers.BlockVisibilityBuffer);
            shader.Dispatch(kernels.CullBlocks, Mathf.CeilToInt((float)buffers.BlockCount / BlockThreadGroupSize), 1, 1);

            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.BlockVisibility, buffers.BlockVisibilityBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.VisibleIndices, buffers.VisibleIndexBuffer);
            shader.SetBuffer(kernels.BuildVisibleInstances, FanlightShaderIds.DrawArgs, buffers.ArgsBuffer);
            shader.Dispatch(kernels.BuildVisibleInstances, Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize), 1, 1);
        }

        public void DispatchAnimation(ComputeShader shader, FanlightGpuKernels kernels, FanlightGpuBuffers buffers, FanlightGpuDispatchContext context, bool visibleOnly)
        {
            SetCommonParams(shader, context, buffers, false);

            var kernel = visibleOnly ? kernels.GenerateVisibleAnimation : kernels.GenerateAllAnimation;
            shader.SetBuffer(kernel, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.VisibleIndices, buffers.VisibleIndexBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.DrawArgs, buffers.ArgsBuffer);
            shader.SetBuffer(kernel, FanlightShaderIds.Matrices, buffers.MatrixBuffer);
            shader.Dispatch(kernel, Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize), 1, 1);
        }

        public void DispatchColors(ComputeShader shader, FanlightGpuKernels kernels, FanlightGpuBuffers buffers, FanlightGpuDispatchContext context)
        {
            SetCommonParams(shader, context, buffers, false);
            SetColorParams(shader, context.Color);

            shader.SetBuffer(kernels.GenerateAllColors, FanlightShaderIds.Seats, buffers.SeatBuffer);
            shader.SetBuffer(kernels.GenerateAllColors, FanlightShaderIds.Colors, buffers.ColorBuffer);
            shader.Dispatch(kernels.GenerateAllColors, Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize), 1, 1);
        }

        private void SetCommonParams(ComputeShader shader, FanlightGpuDispatchContext context, FanlightGpuBuffers buffers, bool includeVisibilityParams)
        {
            var audience = context.Audience;
            var tempo = context.Tempo;
            var motion = context.Motion;

            shader.SetInt(FanlightShaderIds.InstanceCount, buffers.SeatCount);
            shader.SetInt(FanlightShaderIds.BlockCountValue, buffers.BlockCount);
            shader.SetMatrix(FanlightShaderIds.LocalToWorld, context.LocalToWorld);
            shader.SetMatrix(FanlightShaderIds.WorldToLocal, context.WorldToLocal);
            shader.SetFloat(FanlightShaderIds.Time, context.Time);
            shader.SetVector(FanlightShaderIds.Beat, new Vector4(tempo.SongTime, tempo.Beat, tempo.BeatPhase, tempo.BarPhase));
            shader.SetVector(FanlightShaderIds.Tempo, new Vector4(tempo.Enabled ? 1f : 0f, tempo.Bpm, tempo.BeatsPerBar, 0f));

            shader.SetVector(FanlightShaderIds.SeatPitch, new Vector4(audience.seatPitch.x, audience.seatPitch.y, 0f, 0f));
            shader.SetVector(FanlightShaderIds.BlockCount, new Vector4(audience.blockCount.x, audience.blockCount.y, 0f, 0f));

            if (includeVisibilityParams)
            {
                shader.SetFloat(FanlightShaderIds.CullingScale, FanlightGeometryBuilder.GetMaxScale(context.LocalToWorld));
                shader.SetInt(FanlightShaderIds.EnableCulling, context.EnableCulling ? 1 : 0);
                SetFrustumPlanes(shader, context.EnableCulling ? context.CullingCamera : null, context.WorldBounds);
            }

            var worldDirection = ComputeWorldDirection(motion);
            shader.SetVector(FanlightShaderIds.MotionTiming, new Vector4(motion.swing.swingSpeed, motion.swing.randomPhase, motion.noise.phaseIrregularity, motion.noise.phaseIrregularitySpeed));
            // x=armLengthMin, y=armLengthMax, z=minAngle, w=maxAngle
            shader.SetVector(FanlightShaderIds.MotionSwing, new Vector4(motion.swing.armLengthMin, motion.swing.armLengthMax, motion.swing.minAngle, motion.swing.maxAngle));
            // x=peakHold, y=followThrough, z=lean, w=crispness
            shader.SetVector(FanlightShaderIds.MotionShape, new Vector4(motion.swing.peakHold, motion.swing.followThrough, motion.swing.lean, motion.swing.crispness));
            shader.SetInt(FanlightShaderIds.SwingMode, (int)motion.direction.swingMode);
            // x=horizontalRatio, y=wristSwingSpeed, z=wristSwingAngle
            shader.SetVector(FanlightShaderIds.SwingWrist, new Vector4(motion.swing.horizontalRatio, motion.swing.wristSwingSpeed, motion.swing.wristSwingAngle, 0f));
            shader.SetVector(FanlightShaderIds.SwingAxis, new Vector4(worldDirection.x, worldDirection.y, worldDirection.z, motion.direction.directionSpread));
            shader.SetVector(FanlightShaderIds.SwingTargetPos, new Vector4(context.SwingTargetWorldPos.x, context.SwingTargetWorldPos.y, context.SwingTargetWorldPos.z, motion.direction.aimStrength));
            // x=seatJitter, y=heightJitter, z=armLengthJitter, w=angleNoise
            shader.SetVector(FanlightShaderIds.MotionVariation, new Vector4(motion.human.seatJitter, motion.human.heightJitter, motion.human.armLengthJitter, motion.swing.angleNoise));
            shader.SetVector(FanlightShaderIds.MotionNoise, new Vector4(motion.noise.axisNoiseAmount, motion.noise.axisNoiseSpeed, motion.noise.noiseOctaves, motion.noise.noiseDetail));
            shader.SetVector(FanlightShaderIds.MotionHuman, new Vector4(motion.human.enthusiasm, motion.human.enthusiasmVariation, motion.human.reactionDelay, motion.human.speedVariation));
            shader.SetVector(FanlightShaderIds.MotionRest, new Vector4(motion.human.restProbability, motion.human.restMotionLevel, motion.human.lazyFanRatio, 0f));
            shader.SetVector(FanlightShaderIds.MotionRestTiming, new Vector4(motion.human.restCycleDuration, motion.human.restDuration, motion.human.restFadeDuration, motion.human.restPhaseRandomness));
            shader.SetVector(FanlightShaderIds.MotionBeat, new Vector4(motion.beatSync.beatSyncBlend, motion.beatSync.beatsPerSwing, motion.beatSync.beatPhaseOffset, motion.beatSync.downbeatAccent));
            shader.SetVector(FanlightShaderIds.MotionBeatSpread, new Vector4(motion.beatSync.beatReactionDelay, motion.beatSync.beatSeatJitter, motion.beatSync.beatBlockDelay.x, motion.beatSync.beatBlockDelay.y));
            shader.SetFloat(FanlightShaderIds.GripPivotY, buffers.MeshPivotY);
        }

        private void SetColorParams(ComputeShader shader, FanlightColorSettings color)
        {
            shader.SetInt(FanlightShaderIds.ColorMode, (int)color.mode);
            shader.SetVector(FanlightShaderIds.PrimaryColor, color.primaryColor);
            shader.SetVector(FanlightShaderIds.SecondaryColor, color.secondaryColor);
            shader.SetVector(FanlightShaderIds.Brightness, new Vector4(color.intensity, color.randomIntensity, 0f, 0f));
            shader.SetInt(FanlightShaderIds.PaletteColorCount, FillPalette(color));
            shader.SetVectorArray(FanlightShaderIds.PaletteColors, _paletteColors);
        }

        private int FillPalette(FanlightColorSettings color)
        {
            var palette = color.paletteColors;
            var count = Mathf.Clamp(palette?.Length ?? 0, 0, FanlightColorSettings.MaxPaletteColors);

            if (count == 0)
            {
                _paletteColors[0] = color.primaryColor;
                count = 1;
            }
            else
            {
                for (var i = 0; i < count; i++)
                    _paletteColors[i] = palette[i];
            }

            for (var i = count; i < _paletteColors.Length; i++)
                _paletteColors[i] = Color.black;

            return count;
        }

        private void SetFrustumPlanes(ComputeShader shader, Camera cullingCamera, Bounds worldBounds)
        {
            if (cullingCamera == null)
                SetAlwaysVisiblePlanes(worldBounds);
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
