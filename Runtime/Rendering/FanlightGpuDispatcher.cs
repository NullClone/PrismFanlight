using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuDispatcher
    {
        public const int InstanceThreadGroupSize = 128;
        public const int BlockThreadGroupSize = 64;

        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustumPlanes = new Vector4[6];


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
            shader.SetBuffer(kernel, FanlightShaderIds.Colors, buffers.ColorBuffer);
            shader.Dispatch(kernel, Mathf.CeilToInt((float)buffers.SeatCount / InstanceThreadGroupSize), 1, 1);
        }

        private void SetCommonParams(ComputeShader shader, FanlightGpuDispatchContext context, FanlightGpuBuffers buffers, bool includeVisibilityParams)
        {
            var audience = context.Audience;
            var motion = context.Motion;
            var color = context.Color;

            shader.SetInt(FanlightShaderIds.InstanceCount, buffers.SeatCount);
            shader.SetInt(FanlightShaderIds.BlockCountValue, buffers.BlockCount);
            shader.SetMatrix(FanlightShaderIds.LocalToWorld, context.LocalToWorld);
            shader.SetFloat(FanlightShaderIds.Time, context.Time);

            shader.SetVector(FanlightShaderIds.SeatPitch, new Vector4(audience.seatPitch.x, audience.seatPitch.y, 0.0f, 0.0f));
            shader.SetVector(FanlightShaderIds.BlockCount, new Vector4(audience.blockCount.x, audience.blockCount.y, 0.0f, 0.0f));

            if (includeVisibilityParams)
            {
                shader.SetFloat(FanlightShaderIds.CullingScale, FanlightGeometryBuilder.GetMaxScale(context.LocalToWorld));
                shader.SetInt(FanlightShaderIds.EnableCulling, context.EnableCulling ? 1 : 0);
                SetFrustumPlanes(shader, context.EnableCulling ? context.CullingCamera : null, context.WorldBounds);
            }

            shader.SetVector(FanlightShaderIds.MotionTiming, new Vector4(motion.frequency, motion.randomPhase, motion.phaseNoiseAmount, motion.phaseNoiseSpeed));
            shader.SetVector(FanlightShaderIds.MotionSwing, new Vector4(motion.armLength, motion.minAngle, motion.maxAngle, motion.snapAmount));
            shader.SetVector(FanlightShaderIds.MotionVariation, new Vector4(motion.seatJitter, motion.heightJitter, motion.armLengthJitter, 0.0f));
            shader.SetVector(FanlightShaderIds.MotionNoise, new Vector4(motion.axisNoiseAmount, motion.axisNoiseSpeed, 0.0f, 0.0f));
            shader.SetInt(FanlightShaderIds.ColorMode, (int)color.mode);
            shader.SetVector(FanlightShaderIds.PrimaryColor, color.primaryColor);
            shader.SetVector(FanlightShaderIds.SecondaryColor, color.secondaryColor);
            shader.SetVector(FanlightShaderIds.Brightness, new Vector4(color.baseIntensity, color.effectIntensity, color.randomIntensity, color.saturation));
            shader.SetVector(FanlightShaderIds.Hue, new Vector4(color.hueSpeed, color.randomHueAmount, 0.0f, 0.0f));
            shader.SetVector(FanlightShaderIds.Wave, new Vector4(color.waveOrigin.x, color.waveOrigin.y, color.waveFrequency, color.waveSpeed));
            shader.SetVector(FanlightShaderIds.WaveShape, new Vector4(color.waveSharpness, 0.0f, 0.0f, 0.0f));
        }

        private void SetFrustumPlanes(ComputeShader shader, Camera cullingCamera, Bounds worldBounds)
        {
            if (cullingCamera == null)
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
            var radius = bounds.extents.magnitude + 1.0f;

            _frustumPlanes[0] = new Vector4(1, 0, 0, radius - center.x);
            _frustumPlanes[1] = new Vector4(-1, 0, 0, radius + center.x);
            _frustumPlanes[2] = new Vector4(0, 1, 0, radius - center.y);
            _frustumPlanes[3] = new Vector4(0, -1, 0, radius + center.y);
            _frustumPlanes[4] = new Vector4(0, 0, 1, radius - center.z);
            _frustumPlanes[5] = new Vector4(0, 0, -1, radius + center.z);
        }
    }
}
