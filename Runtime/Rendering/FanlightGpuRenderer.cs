using UnityEngine;
using UnityEngine.Profiling;

namespace PrismFanlight.Rendering
{
    public sealed class FanlightGpuRenderer
    {
        // Fields

        private readonly FanlightGpuBuffers _buffers = new();
        private readonly FanlightGpuDispatcher _dispatcher = new();
        private readonly FanlightGpuDebugReadback _debugReadback = new();

        private MaterialPropertyBlock _properties;
        private FanlightGpuKernels _kernels;
        private Audience _audience;
        private Mesh _mesh;
        private bool _isInitialized;


        // Properties

        public bool IsReady => _isInitialized;

        public int SeatCount => _buffers.SeatCount;

        public int BlockCount => _buffers.BlockCount;

        public int LastVisibleSeatCount => _debugReadback.LastVisibleSeatCount;

        public int LastCulledSeatCount => Mathf.Max(0, SeatCount - LastVisibleSeatCount);

        public int InstanceThreadGroups => Mathf.CeilToInt((float)SeatCount / FanlightGpuDispatcher.InstanceThreadGroupSize);

        public int BlockThreadGroups => Mathf.CeilToInt((float)BlockCount / FanlightGpuDispatcher.BlockThreadGroupSize);

        public long BufferMemoryBytes => _buffers.EstimateMemoryBytes();


        // Methods

        public void Render(
            Mesh mesh,
            Material material,
            ComputeShader computeShader,
            Camera cullingCamera,
            bool enableCulling,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time)
        {
            if (!CanRender(mesh, material, computeShader, audience)) return;

            EnsureInitialized(mesh, computeShader, audience);

            var worldBounds = FanlightGeometryBuilder.TransformBounds(localToWorld, _buffers.LocalBounds);
            var context = new FanlightGpuDispatchContext(
                cullingCamera,
                enableCulling,
                audience,
                motion,
                color,
                localToWorld,
                time,
                worldBounds);

            Profiler.BeginSample("Prism Fanlight GPU Cull/Generate");
            _dispatcher.Dispatch(computeShader, _kernels, _buffers, context);
            _debugReadback.Request(_buffers.ArgsBuffer, _buffers.SeatCount);
            Profiler.EndSample();

            Profiler.BeginSample("Prism Fanlight GPU Draw");
            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            _properties.SetBuffer(FanlightShaderIds.Colors, _buffers.ColorBuffer);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.VisibleIndexBuffer);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, worldBounds, _buffers.ArgsBuffer, 0, _properties);
            Profiler.EndSample();
        }

        public void Dispose()
        {
            _buffers.Release();
            _debugReadback.Reset();
            _properties = null;
            _mesh = null;
            _isInitialized = false;
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
            if (_isInitialized && _mesh == mesh && _buffers.SeatCount == audience.TotalSeatCount && audience.Equals(_audience)) return;

            Dispose();

            _mesh = mesh;
            _audience = audience;
            _kernels = new FanlightGpuKernels(computeShader);
            _properties = new MaterialPropertyBlock();
            _buffers.Allocate(mesh, audience);
            _isInitialized = true;
        }
    }
}
