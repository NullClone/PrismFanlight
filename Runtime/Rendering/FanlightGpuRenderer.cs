using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace PrismFanlight.Rendering
{
    public sealed class FanlightGpuRenderer
    {
        private const int InstanceThreadGroupSize = 128;
        private const int BlockThreadGroupSize = 64;

        private static readonly int SeatsId = Shader.PropertyToID("_Seats");
        private static readonly int BlocksId = Shader.PropertyToID("_Blocks");
        private static readonly int BlockVisibilityId = Shader.PropertyToID("_BlockVisibility");
        private static readonly int VisibleIndicesId = Shader.PropertyToID("_VisibleIndices");
        private static readonly int DrawArgsId = Shader.PropertyToID("_DrawArgs");
        private static readonly int MatricesId = Shader.PropertyToID("_FanlightMatrices");
        private static readonly int ColorsId = Shader.PropertyToID("_FanlightColors");
        private static readonly int InstanceCountId = Shader.PropertyToID("_InstanceCount");
        private static readonly int BlockCountValueId = Shader.PropertyToID("_BlockCountValue");
        private static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
        private static readonly int TimeId = Shader.PropertyToID("_FanlightTime");
        private static readonly int FrustumPlanesId = Shader.PropertyToID("_FrustumPlanes");
        private static readonly int CullingScaleId = Shader.PropertyToID("_CullingScale");
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
        private ComputeBuffer _blockBuffer;
        private ComputeBuffer _blockVisibilityBuffer;
        private ComputeBuffer _visibleIndexBuffer;
        private ComputeBuffer _matrixBuffer;
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _argsBuffer;
        private MaterialPropertyBlock _properties;
        private FanlightSeatData[] _seatData;
        private FanlightBlockData[] _blockData;
        private Audience _audience;
        private Mesh _mesh;
        private Bounds _bounds;
        private int _clearKernel;
        private int _cullBlocksKernel;
        private int _generateVisibleKernel;
        private int _seatCount;
        private int _blockCount;
        private int _lastVisibleSeatCount;
        private int _lastReadbackFrame = -1;
        private bool _readbackPending;
        private bool _isInitialized;

        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _frustumPlanes = new Vector4[6];


        public bool IsReady => _isInitialized;

        public int SeatCount => _seatCount;

        public int BlockCount => _blockCount;

        public int LastVisibleSeatCount => _lastVisibleSeatCount;

        public int LastCulledSeatCount => Mathf.Max(0, _seatCount - _lastVisibleSeatCount);

        public int InstanceThreadGroups => Mathf.CeilToInt((float)_seatCount / InstanceThreadGroupSize);

        public int BlockThreadGroups => Mathf.CeilToInt((float)_blockCount / BlockThreadGroupSize);

        public long BufferMemoryBytes => EstimateBufferMemoryBytes();


        public void Render(
            Mesh mesh,
            Material material,
            ComputeShader computeShader,
            Camera cullingCamera,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time)
        {
            if (!CanRender(mesh, material, cullingCamera, computeShader, audience)) return;

            EnsureInitialized(mesh, computeShader, audience);

            var worldBounds = TransformBounds(localToWorld, _bounds);

            Profiler.BeginSample("Prism Fanlight GPU Cull/Generate");

            Dispatch(computeShader, cullingCamera, audience, motion, color, localToWorld, time, worldBounds);
            RequestVisibleCountReadback();

            Profiler.EndSample();

            Profiler.BeginSample("Prism Fanlight GPU Draw");

            _properties.SetBuffer(MatricesId, _matrixBuffer);
            _properties.SetBuffer(ColorsId, _colorBuffer);
            _properties.SetBuffer(VisibleIndicesId, _visibleIndexBuffer);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, worldBounds, _argsBuffer, 0, _properties);

            Profiler.EndSample();
        }

        public void Dispose()
        {
            _seatBuffer?.Release();
            _blockBuffer?.Release();
            _blockVisibilityBuffer?.Release();
            _visibleIndexBuffer?.Release();
            _matrixBuffer?.Release();
            _colorBuffer?.Release();
            _argsBuffer?.Release();

            _seatBuffer = null;
            _blockBuffer = null;
            _blockVisibilityBuffer = null;
            _visibleIndexBuffer = null;
            _matrixBuffer = null;
            _colorBuffer = null;
            _argsBuffer = null;
            _properties = null;
            _seatData = null;
            _blockData = null;
            _mesh = null;
            _isInitialized = false;
            _seatCount = 0;
            _blockCount = 0;
            _lastVisibleSeatCount = 0;
            _readbackPending = false;
        }

        private static bool CanRender(Mesh mesh, Material material, Camera cullingCamera, ComputeShader computeShader, Audience audience)
        {
            return mesh != null
                   && material != null
                   && computeShader != null
                   && cullingCamera != null
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
            _blockCount = audience.blockCount.x * audience.blockCount.y;
            _clearKernel = computeShader.FindKernel("ClearIndirectArgs");
            _cullBlocksKernel = computeShader.FindKernel("CullBlocks");
            _generateVisibleKernel = computeShader.FindKernel("GenerateVisibleInstances");
            _properties = new MaterialPropertyBlock();

            _seatData = BuildSeatData(audience);
            _blockData = BuildBlockData(audience, mesh);
            _bounds = BuildBounds(audience, mesh);

            _seatBuffer = new ComputeBuffer(_seatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            _blockBuffer = new ComputeBuffer(_blockCount, FanlightBlockData.Stride, ComputeBufferType.Structured);
            _blockVisibilityBuffer = new ComputeBuffer(_blockCount, sizeof(uint), ComputeBufferType.Structured);
            _visibleIndexBuffer = new ComputeBuffer(_seatCount, sizeof(uint), ComputeBufferType.Structured);
            _matrixBuffer = new ComputeBuffer(_seatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            _colorBuffer = new ComputeBuffer(_seatCount, sizeof(float) * 4, ComputeBufferType.Structured);
            _argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

            _seatBuffer.SetData(_seatData);
            _blockBuffer.SetData(_blockData);
            _argsBuffer.SetData(new[]
            {
                mesh.GetIndexCount(0),
                0u,
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

        private static FanlightBlockData[] BuildBlockData(Audience audience, Mesh mesh)
        {
            var data = new FanlightBlockData[audience.blockCount.x * audience.blockCount.y];
            var blockSeatCount = audience.BlockSeatCount;
            var meshPadding = mesh.bounds.size.magnitude + 4.0f;

            for (var by = 0; by < audience.blockCount.y; by++)
            {
                for (var bx = 0; bx < audience.blockCount.x; bx++)
                {
                    var block = math.int2(bx, by);
                    var min = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
                    var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;
                    var center2 = (min + max) * 0.5f;
                    var size2 = math.max(max - min, math.float2(0.01f, 0.01f));
                    var radius = math.length(math.float3(size2.x, 8.0f, size2.y) * 0.5f) + meshPadding;
                    var blockIndex = by * audience.blockCount.x + bx;

                    data[blockIndex] = new FanlightBlockData(
                        new Vector3(center2.x, 0.0f, center2.y),
                        radius,
                        blockIndex * blockSeatCount,
                        blockSeatCount);
                }
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
            Camera cullingCamera,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time,
            Bounds worldBounds)
        {
            SetCommonParams(computeShader, cullingCamera, audience, motion, color, localToWorld, time, worldBounds);

            computeShader.SetBuffer(_clearKernel, DrawArgsId, _argsBuffer);
            computeShader.Dispatch(_clearKernel, 1, 1, 1);

            computeShader.SetBuffer(_cullBlocksKernel, BlocksId, _blockBuffer);
            computeShader.SetBuffer(_cullBlocksKernel, BlockVisibilityId, _blockVisibilityBuffer);
            var blockGroups = Mathf.CeilToInt((float)_blockCount / BlockThreadGroupSize);
            computeShader.Dispatch(_cullBlocksKernel, blockGroups, 1, 1);

            computeShader.SetBuffer(_generateVisibleKernel, SeatsId, _seatBuffer);
            computeShader.SetBuffer(_generateVisibleKernel, BlockVisibilityId, _blockVisibilityBuffer);
            computeShader.SetBuffer(_generateVisibleKernel, VisibleIndicesId, _visibleIndexBuffer);
            computeShader.SetBuffer(_generateVisibleKernel, DrawArgsId, _argsBuffer);
            computeShader.SetBuffer(_generateVisibleKernel, MatricesId, _matrixBuffer);
            computeShader.SetBuffer(_generateVisibleKernel, ColorsId, _colorBuffer);
            var instanceGroups = Mathf.CeilToInt((float)_seatCount / InstanceThreadGroupSize);
            computeShader.Dispatch(_generateVisibleKernel, instanceGroups, 1, 1);
        }

        private void SetCommonParams(
            ComputeShader computeShader,
            Camera cullingCamera,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time,
            Bounds worldBounds)
        {
            computeShader.SetInt(InstanceCountId, _seatCount);
            computeShader.SetInt(BlockCountValueId, _blockCount);
            computeShader.SetMatrix(LocalToWorldId, localToWorld);
            computeShader.SetFloat(TimeId, time);
            computeShader.SetFloat(CullingScaleId, GetMaxScale(localToWorld));
            SetFrustumPlanes(computeShader, cullingCamera, worldBounds);
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
        }

        private void SetFrustumPlanes(ComputeShader computeShader, Camera cullingCamera, Bounds worldBounds)
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

            computeShader.SetVectorArray(FrustumPlanesId, _frustumPlanes);
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

        private static float GetMaxScale(Matrix4x4 matrix)
        {
            var x = matrix.MultiplyVector(Vector3.right).magnitude;
            var y = matrix.MultiplyVector(Vector3.up).magnitude;
            var z = matrix.MultiplyVector(Vector3.forward).magnitude;
            return Mathf.Max(x, Mathf.Max(y, z));
        }

        private void RequestVisibleCountReadback()
        {
            if (_argsBuffer == null || _readbackPending) return;
            if (Time.frameCount == _lastReadbackFrame) return;
            if (Time.frameCount % 10 != 0) return;

            _readbackPending = true;
            _lastReadbackFrame = Time.frameCount;

            AsyncGPUReadback.Request(_argsBuffer, request =>
            {
                _readbackPending = false;

                if (request.hasError) return;

                var args = request.GetData<uint>();
                if (args.Length > 1)
                {
                    _lastVisibleSeatCount = (int)math.min(args[1], (uint)_seatCount);
                }
            });
        }

        private long EstimateBufferMemoryBytes()
        {
            if (!_isInitialized) return 0;

            return (long)_seatCount * FanlightSeatData.Stride
                   + (long)_blockCount * FanlightBlockData.Stride
                   + (long)_blockCount * sizeof(uint)
                   + (long)_seatCount * sizeof(uint)
                   + (long)_seatCount * sizeof(float) * 16
                   + (long)_seatCount * sizeof(float) * 4
                   + sizeof(uint) * 5;
        }
    }
}
