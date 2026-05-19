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
        private readonly FanlightGpuUpdateScheduler _scheduler = new();

        private MaterialPropertyBlock _properties;
        private FanlightGpuKernels _kernels;
        private Audience _audience;
        private Mesh _mesh;
        private ComputeShader _computeShader;
        private bool _isInitialized;
        private bool _animationInitialized;
        private Matrix4x4 _lastAnimationLocalToWorld;


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
            uint renderingLayerMask,
            Camera cullingCamera,
            bool enableCulling,
            FanlightGpuUpdateTiming visibilityUpdate,
            FanlightGpuUpdateTiming animationUpdate,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time,
            float updateClock)
        {
            var validatedAudience = (audience ?? Audience.Default()).Validated();

            if (!CanRender(mesh, material, computeShader, validatedAudience)) return;

            EnsureInitialized(mesh, computeShader, validatedAudience);

            var worldBounds = FanlightGeometryBuilder.TransformBounds(localToWorld, _buffers.LocalBounds);

            var context = new FanlightGpuDispatchContext(
                cullingCamera,
                enableCulling,
                validatedAudience,
                motion,
                color,
                localToWorld,
                time,
                worldBounds);

            if (_scheduler.ShouldUpdateVisibility(visibilityUpdate, updateClock))
            {
                Profiler.BeginSample("Prism Fanlight GPU Visibility");
                _dispatcher.DispatchVisibility(computeShader, _kernels, _buffers, context);
                _debugReadback.Request(_buffers.ArgsBuffer, _buffers.SeatCount);
                Profiler.EndSample();
            }

            var refreshAllAnimation = !_animationInitialized || localToWorld != _lastAnimationLocalToWorld;

            if (_scheduler.ShouldUpdateAnimation(animationUpdate, updateClock, refreshAllAnimation))
            {
                Profiler.BeginSample("Prism Fanlight GPU Animation");
                _dispatcher.DispatchAnimation(computeShader, _kernels, _buffers, context, !refreshAllAnimation);
                _animationInitialized = true;
                _lastAnimationLocalToWorld = localToWorld;
                Profiler.EndSample();
            }

            Profiler.BeginSample("Prism Fanlight GPU Draw");
            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            _properties.SetBuffer(FanlightShaderIds.Colors, _buffers.ColorBuffer);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.VisibleIndexBuffer);

            var renderParams = new RenderParams(material)
            {
                worldBounds = worldBounds,
                matProps = _properties,
                renderingLayerMask = renderingLayerMask
            };

            Graphics.RenderMeshIndirect(renderParams, mesh, _buffers.ArgsBuffer);
            Profiler.EndSample();
        }

        public void Dispose()
        {
            _buffers.Release();
            _debugReadback.Reset();
            _properties = null;
            _mesh = null;
            _computeShader = null;
            _isInitialized = false;
            _animationInitialized = false;
            _lastAnimationLocalToWorld = Matrix4x4.identity;
            _scheduler.Reset();
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
            if (_isInitialized
                && _mesh == mesh
                && _computeShader == computeShader
                && _buffers.SeatCount == audience.TotalSeatCount
                && audience.Equals(_audience))
            {
                return;
            }

            Dispose();

            _mesh = mesh;
            _computeShader = computeShader;
            _audience = audience;
            _kernels = new FanlightGpuKernels(computeShader);
            _properties = new MaterialPropertyBlock();
            _buffers.Allocate(mesh, audience);
            _isInitialized = true;
        }
    }
}
