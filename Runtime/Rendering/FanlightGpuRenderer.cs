using UnityEngine;
using UnityEngine.Profiling;

namespace PrismFanlight.Rendering
{
    public sealed class FanlightGpuRenderer
    {
        // Fields

        private readonly FanlightGpuBuffers _buffers = new();
        private readonly FanlightGpuDispatcher _dispatcher = new();
        private readonly FanlightGpuVisibilityReadback _visibilityReadback = new();
        private readonly FanlightGpuUpdateScheduler _scheduler = new();
        private readonly Vector4[] _paletteColors = new Vector4[FanlightColorSettings.PaletteSlotCount];

        private MaterialPropertyBlock _properties;
        private MaterialPropertyBlock _audienceProperties;
        private FanlightGpuKernels _kernels;
        private SeatLayout _layout;
        private Mesh _mesh;
        private ComputeShader _computeShader;
        private bool _audienceAllocated;
        private bool _isInitialized;
        private bool _animationInitialized;
        private bool _hasLastUpdateClock;
        private int _lastRandomHash;
        private float _lastUpdateClock;
        private Matrix4x4 _lastAnimationLocalToWorld;


        // Properties

        public bool IsReady => _isInitialized;

        public int VisibleSeatCount => _visibilityReadback.VisibleSeatCount;


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
            SeatLayout layout,
            Material audienceMaterial,
            FanlightResolvedState state,
            bool isTimeJump,
            Vector3 lodCameraWorldPos)
        {
            if (!CanRender(mesh, material, computeShader, layout))
            {
                Dispose();
                return;
            }

            var audienceEnabled = state.Audience.enabled && audienceMaterial != null;

            EnsureInitialized(mesh, computeShader, layout, audienceEnabled, state.Random);

            var randomHash = state.Random.GetStableHash();
            if (_lastRandomHash != randomHash)
            {
                _buffers.UpdateRandomData(state.Random);
                _lastRandomHash = randomHash;
                _animationInitialized = false;
            }

            var worldBounds = FanlightGeometryBuilder.TransformBounds(state.LocalToWorld, _buffers.LocalBounds);

            var context = new FanlightGpuDispatchContext(
                cullingCamera,
                enableCulling,
                layout,
                state.Tempo,
                state.Motion,
                state.Audience,
                state.Lod,
                state.SwingTargetWorldPosition,
                lodCameraWorldPos,
                state.LocalToWorld,
                state.Time,
                worldBounds);

            if (isTimeJump)
            {
                _scheduler.Reset();
                _animationInitialized = false;
            }
            else if (_hasLastUpdateClock && state.UpdateClock < _lastUpdateClock)
            {
                _scheduler.Reset();
            }

            var refreshAllAnimation = !_animationInitialized || state.LocalToWorld != _lastAnimationLocalToWorld;
            var visibilityUpdated = refreshAllAnimation || _scheduler.ShouldUpdateVisibility(visibilityUpdate, state.UpdateClock);

            if (visibilityUpdated)
            {
                Profiler.BeginSample("Prism Fanlight GPU Visibility");
                _dispatcher.DispatchVisibility(computeShader, _kernels, _buffers, context);
                _visibilityReadback.Request(_buffers.PenlightArgsBuffer, _buffers.SeatCount);
                Profiler.EndSample();
            }

            if (_scheduler.ShouldUpdateAnimation(animationUpdate, state.UpdateClock, refreshAllAnimation || visibilityUpdated))
            {
                Profiler.BeginSample("Prism Fanlight GPU Animation");
                _dispatcher.DispatchAnimation(computeShader, _kernels, _buffers, context, !refreshAllAnimation);
                _animationInitialized = true;
                _lastAnimationLocalToWorld = state.LocalToWorld;
                Profiler.EndSample();
            }

            _hasLastUpdateClock = true;
            _lastUpdateClock = state.UpdateClock;

            Profiler.BeginSample("Prism Fanlight GPU Draw");
            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            _properties.SetBuffer(FanlightShaderIds.ColorAssignments, _buffers.ColorAssignmentBuffer);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            _properties.SetBuffer(FanlightShaderIds.PenlightVisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            SetColorProperties(_properties, state.Color);

            var renderParams = new RenderParams(material)
            {
                renderingLayerMask = renderingLayerMask,
                receiveShadows = false,
                worldBounds = worldBounds,
                matProps = _properties
            };

            Graphics.RenderMeshIndirect(renderParams, mesh, _buffers.PenlightArgsBuffer);
            Profiler.EndSample();

            if (audienceEnabled)
            {
                var audienceBounds = worldBounds;
                audienceBounds.Expand(2.0f);
                DrawAudience(audienceMaterial, renderingLayerMask, audienceBounds, state.Color);
            }
        }

        private static bool CanRender(Mesh mesh, Material material, ComputeShader computeShader, SeatLayout layout)
        {
            return mesh != null
                   && material != null
                   && computeShader != null
                   && layout != null
                   && layout.TotalSeatCount > 0
                   && layout.BlockSeatCount > 0;
        }

        private void DrawAudience(Material audienceMaterial, uint renderingLayerMask, Bounds worldBounds, FanlightColorSettings color)
        {
            Profiler.BeginSample("Prism Fanlight GPU Audience Draw");

            _audienceProperties ??= new MaterialPropertyBlock();
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceParts, _buffers.AudiencePartBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.AudienceVisibleIndexBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceVisibleIndices, _buffers.AudienceVisibleIndexBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.ColorAssignments, _buffers.ColorAssignmentBuffer);
            SetColorProperties(_audienceProperties, color);

            var renderParams = new RenderParams(audienceMaterial)
            {
                renderingLayerMask = renderingLayerMask,
                receiveShadows = false,
                worldBounds = worldBounds,
                matProps = _audienceProperties
            };

            Graphics.RenderMeshIndirect(renderParams, FanlightGeometryBuilder.GetAudienceQuad(), _buffers.AudienceArgsBuffer);
            Profiler.EndSample();
        }

        private void EnsureInitialized(Mesh mesh, ComputeShader computeShader, SeatLayout layout, bool allocateAudience, FanlightRandomSettings random)
        {
            if (_isInitialized
                && _mesh == mesh
                && _computeShader == computeShader
                && _audienceAllocated == allocateAudience
                && _buffers.SeatCount == layout.TotalSeatCount
                && layout.Equals(_layout))
            {
                return;
            }

            Dispose();

            _mesh = mesh;
            _computeShader = computeShader;
            _layout = layout;
            _kernels = new FanlightGpuKernels(computeShader);
            _properties = new MaterialPropertyBlock();
            _buffers.Allocate(mesh, layout, allocateAudience, random);
            _audienceAllocated = allocateAudience;
            _lastRandomHash = random.GetStableHash();
            _isInitialized = true;
        }

        private void SetColorProperties(MaterialPropertyBlock properties, FanlightColorSettings color)
        {
            var settings = color.Validated();
            for (var i = 0; i < FanlightColorSettings.PaletteSlotCount; i++)
            {
                _paletteColors[i] = settings.GetSlot(i);
            }

            properties.SetVectorArray(FanlightShaderIds.PaletteColors, _paletteColors);
            properties.SetFloat(FanlightShaderIds.GlobalIntensity, settings.GetGlobalIntensity());
            properties.SetFloat(FanlightShaderIds.RandomIntensity, settings.randomIntensity);
        }

        public void Dispose()
        {
            _buffers.Release();
            _visibilityReadback.Reset();
            _properties = null;
            _audienceProperties = null;
            _audienceAllocated = false;
            _mesh = null;
            _computeShader = null;
            _isInitialized = false;
            _animationInitialized = false;
            _hasLastUpdateClock = false;
            _lastRandomHash = 0;
            _lastUpdateClock = 0.0f;
            _lastAnimationLocalToWorld = Matrix4x4.identity;
            _scheduler.Reset();
        }
    }
}
