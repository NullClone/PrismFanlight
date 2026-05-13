using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace PrismFanlight.Rendering
{
    public sealed class FanlightGpuRenderer
    {
        // Fields

        private const int ThreadGroupSize = 128;

        private static readonly int SeatsId = Shader.PropertyToID("_Seats");
        private static readonly int MatricesId = Shader.PropertyToID("_FanlightMatrices");
        private static readonly int ColorsId = Shader.PropertyToID("_FanlightColors");
        private static readonly int InstanceCountId = Shader.PropertyToID("_InstanceCount");
        private static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
        private static readonly int TimeId = Shader.PropertyToID("_FanlightTime");

        private static readonly int SeatPitchId = Shader.PropertyToID("_SeatPitch");
        private static readonly int BlockCountId = Shader.PropertyToID("_BlockCount");
        private static readonly int MotionTimingId = Shader.PropertyToID("_MotionTiming");
        private static readonly int MotionSwingId = Shader.PropertyToID("_MotionSwing");
        private static readonly int MotionVariationId = Shader.PropertyToID("_MotionVariation");
        private static readonly int MotionNoiseId = Shader.PropertyToID("_MotionNoise");
        private static readonly int ColorModeId = Shader.PropertyToID("_ColorMode");
        private static readonly int PrimaryColorId = Shader.PropertyToID("_PrimaryColor");
        private static readonly int SecondaryColorId = Shader.PropertyToID("_SecondaryColor");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int HueId = Shader.PropertyToID("_Hue");
        private static readonly int WaveId = Shader.PropertyToID("_Wave");
        private static readonly int WaveShapeId = Shader.PropertyToID("_WaveShape");

        private ComputeBuffer _seatBuffer;
        private ComputeBuffer _matrixBuffer;
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _argsBuffer;
        private MaterialPropertyBlock _properties;
        private FanlightSeatData[] _seatData;
        private Audience _audience;
        private Mesh _mesh;
        private Bounds _bounds;
        private int _kernel;
        private int _seatCount;
        private bool _isInitialized;


        // Properties

        public bool IsReady => _isInitialized;

        public int SeatCount => _seatCount;

        public Bounds Bounds => _bounds;


        // Methods

        public void Render(
            Mesh mesh,
            Material material,
            ComputeShader computeShader,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time)
        {
            if (!CanRender(mesh, material, computeShader, audience)) return;

            EnsureInitialized(mesh, computeShader, audience);

            Profiler.BeginSample("Prism Fanlight GPU Generate");

            Dispatch(computeShader, audience, motion, color, localToWorld, time);

            Profiler.EndSample();

            Profiler.BeginSample("Prism Fanlight GPU Draw");

            _properties.SetBuffer(MatricesId, _matrixBuffer);
            _properties.SetBuffer(ColorsId, _colorBuffer);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, TransformBounds(localToWorld, _bounds), _argsBuffer, 0, _properties);

            Profiler.EndSample();
        }

        private static bool CanRender(Mesh mesh, Material material, ComputeShader computeShader, Audience audience)
        {
            return mesh != null
                   && material != null
                   && computeShader != null
                   && audience.TotalSeatCount > 0
                   && audience.BlockSeatCount > 0;
        }

        private void EnsureInitialized(Mesh mesh, ComputeShader computeShader, Audience audience)
        {
            if (_isInitialized && _mesh == mesh && _seatCount == audience.TotalSeatCount && audience.Equals(_audience)) return;

            Dispose();

            _mesh = mesh;
            _audience = audience;
            _seatCount = audience.TotalSeatCount;
            _kernel = computeShader.FindKernel("GenerateInstances");
            _properties = new MaterialPropertyBlock();

            _seatData = BuildSeatData(audience);
            _bounds = BuildBounds(audience, mesh);

            _seatBuffer = new ComputeBuffer(_seatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            _matrixBuffer = new ComputeBuffer(_seatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            _colorBuffer = new ComputeBuffer(_seatCount, sizeof(float) * 4, ComputeBufferType.Structured);
            _argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

            _seatBuffer.SetData(_seatData);
            _argsBuffer.SetData(new[]
            {
                mesh.GetIndexCount(0),
                (uint)_seatCount,
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0u
            });

            _isInitialized = true;
        }

        private static FanlightSeatData[] BuildSeatData(Audience audience)
        {
            var data = new FanlightSeatData[audience.TotalSeatCount];

            for (var i = 0; i < data.Length; i++)
            {
                var (block, seat) = audience.GetCoordinatesFromIndex(i);
                var planePosition = audience.GetPositionOnPlane(block, seat);
                var localPosition = new Vector3(planePosition.x, 0.0f, planePosition.y);
                data[i] = new FanlightSeatData(
                    localPosition,
                    new Vector2(planePosition.x, planePosition.y),
                    new Vector2(block.x, block.y),
                    (uint)i * 2u + 123u);
            }

            return data;
        }

        private static Bounds BuildBounds(Audience audience, Mesh mesh)
        {
            var min = new float2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new float2(float.NegativeInfinity, float.NegativeInfinity);

            for (var bx = 0; bx < audience.blockCount.x; bx++)
            {
                for (var by = 0; by < audience.blockCount.y; by++)
                {
                    var block = math.int2(bx, by);
                    min = math.min(min, audience.GetPositionOnPlane(block, math.int2(0, 0)));
                    max = math.max(max, audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)));
                }
            }

            var center = new Vector3((min.x + max.x) * 0.5f, 0.0f, (min.y + max.y) * 0.5f);
            var size = new Vector3(math.max(max.x - min.x, 1.0f), 8.0f, math.max(max.y - min.y, 1.0f));
            var meshPadding = mesh.bounds.size.magnitude + 4.0f;
            size += Vector3.one * meshPadding;
            return new Bounds(center, size);
        }

        private void Dispatch(
            ComputeShader computeShader,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time)
        {
            computeShader.SetInt(InstanceCountId, _seatCount);
            computeShader.SetMatrix(LocalToWorldId, localToWorld);
            computeShader.SetFloat(TimeId, time);
            computeShader.SetVector(SeatPitchId, new Vector4(audience.seatPitch.x, audience.seatPitch.y, 0.0f, 0.0f));
            computeShader.SetVector(BlockCountId, new Vector4(audience.blockCount.x, audience.blockCount.y, 0.0f, 0.0f));
            computeShader.SetVector(MotionTimingId, new Vector4(motion.frequency, motion.randomPhase, motion.phaseNoiseAmount, motion.phaseNoiseSpeed));
            computeShader.SetVector(MotionSwingId, new Vector4(motion.armLength, motion.minAngle, motion.maxAngle, motion.snapAmount));
            computeShader.SetVector(MotionVariationId, new Vector4(motion.seatJitter, motion.heightJitter, motion.armLengthJitter, 0.0f));
            computeShader.SetVector(MotionNoiseId, new Vector4(motion.axisNoiseAmount, motion.axisNoiseSpeed, 0.0f, 0.0f));
            computeShader.SetInt(ColorModeId, (int)color.mode);
            computeShader.SetVector(PrimaryColorId, color.primaryColor);
            computeShader.SetVector(SecondaryColorId, color.secondaryColor);
            computeShader.SetVector(BrightnessId, new Vector4(color.baseIntensity, color.effectIntensity, color.randomIntensity, color.saturation));
            computeShader.SetVector(HueId, new Vector4(color.hueSpeed, color.randomHueAmount, 0.0f, 0.0f));
            computeShader.SetVector(WaveId, new Vector4(color.waveOrigin.x, color.waveOrigin.y, color.waveFrequency, color.waveSpeed));
            computeShader.SetVector(WaveShapeId, new Vector4(color.waveSharpness, 0.0f, 0.0f, 0.0f));

            computeShader.SetBuffer(_kernel, SeatsId, _seatBuffer);
            computeShader.SetBuffer(_kernel, MatricesId, _matrixBuffer);
            computeShader.SetBuffer(_kernel, ColorsId, _colorBuffer);

            var groups = Mathf.CeilToInt((float)_seatCount / ThreadGroupSize);
            computeShader.Dispatch(_kernel, groups, 1, 1);
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            var center = matrix.MultiplyPoint3x4(bounds.center);
            var extents = bounds.extents;

            var axisX = matrix.MultiplyVector(new Vector3(extents.x, 0.0f, 0.0f));
            var axisY = matrix.MultiplyVector(new Vector3(0.0f, extents.y, 0.0f));
            var axisZ = matrix.MultiplyVector(new Vector3(0.0f, 0.0f, extents.z));

            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds(center, extents * 2.0f);
        }

        public void Dispose()
        {
            _seatBuffer?.Release();
            _matrixBuffer?.Release();
            _colorBuffer?.Release();
            _argsBuffer?.Release();

            _seatBuffer = null;
            _matrixBuffer = null;
            _colorBuffer = null;
            _argsBuffer = null;
            _seatData = null;
            _mesh = null;
            _isInitialized = false;
            _seatCount = 0;
        }
    }
}
