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

        private MaterialPropertyBlock _properties;
        private MaterialPropertyBlock _audienceProperties;
        private FanlightGpuKernels _kernels;
        private SeatLayout _layout;
        private Mesh _mesh;
        private ComputeShader _computeShader;
        private bool _audienceAllocated;
        private bool _isInitialized;
        private bool _animationInitialized;
        private bool _instanceColorsInitialized;
        private int _lastInstanceColorHash;
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
            FanlightTempoState tempo,
            SeatLayout layout,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            FanlightAudienceSettings audience,
            Material audienceMaterial,
            Vector3 swingTargetWorldPos,
            Matrix4x4 localToWorld,
            float time,
            float updateClock)
        {
            var validatedLayout = (layout ?? SeatLayout.Default()).Validated();

            if (!CanRender(mesh, material, computeShader, validatedLayout))
            {
                Dispose();
                return;
            }

            var audienceEnabled = audience.enabled && audienceMaterial != null;
            var handBaseHeight = audienceEnabled ? audience.bodyHeight * audience.shoulderHeight : 0f;

            EnsureInitialized(mesh, computeShader, validatedLayout, audienceEnabled);

            var worldBounds = FanlightGeometryBuilder.TransformBounds(localToWorld, _buffers.LocalBounds);

            var context = new FanlightGpuDispatchContext(
                cullingCamera,
                enableCulling,
                validatedLayout,
                tempo,
                motion,
                color,
                audience,
                handBaseHeight,
                swingTargetWorldPos,
                localToWorld,
                time,
                worldBounds);

            if (_scheduler.ShouldUpdateVisibility(visibilityUpdate, updateClock))
            {
                Profiler.BeginSample("Prism Fanlight GPU Visibility");
                _dispatcher.DispatchVisibility(computeShader, _kernels, _buffers, context);
                _visibilityReadback.Request(_buffers.ArgsBuffer, _buffers.SeatCount);

                if (audienceEnabled)
                {
                    _dispatcher.DispatchAudienceArgs(computeShader, _kernels, _buffers);
                }

                Profiler.EndSample();
            }

            var refreshAllAnimation = !_animationInitialized || localToWorld != _lastAnimationLocalToWorld;

            if (_scheduler.ShouldUpdateAnimation(animationUpdate, updateClock, refreshAllAnimation))
            {
                Profiler.BeginSample("Prism Fanlight GPU Animation");
                _dispatcher.DispatchAnimation(computeShader, _kernels, _buffers, context, !refreshAllAnimation);

                if (audienceEnabled)
                {
                    _dispatcher.DispatchAudience(computeShader, _kernels, _buffers, context, !refreshAllAnimation);
                }

                _animationInitialized = true;
                _lastAnimationLocalToWorld = localToWorld;
                Profiler.EndSample();
            }

            if (ShouldUpdateInstanceColors(color))
            {
                Profiler.BeginSample("Prism Fanlight GPU Colors");
                _dispatcher.DispatchColors(computeShader, _kernels, _buffers, context);
                _instanceColorsInitialized = true;
                _lastInstanceColorHash = color.GetStableHash();
                Profiler.EndSample();
            }

            Profiler.BeginSample("Prism Fanlight GPU Draw");
            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            _properties.SetBuffer(FanlightShaderIds.Colors, _buffers.ColorBuffer);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.VisibleIndexBuffer);
            _properties.SetInt(FanlightShaderIds.ColorSource, color.mode == FanlightColorMode.Single ? 0 : 1);
            _properties.SetColor(FanlightShaderIds.GlobalColor, color.GetGlobalColor());
            _properties.SetFloat(FanlightShaderIds.GlobalIntensity, color.GetGlobalIntensity());

            var renderParams = new RenderParams(material)
            {
                renderingLayerMask = renderingLayerMask,
                receiveShadows = false,
                worldBounds = worldBounds,
                matProps = _properties
            };

            Graphics.RenderMeshIndirect(renderParams, mesh, _buffers.ArgsBuffer);
            Profiler.EndSample();

            if (audienceEnabled)
            {
                DrawAudience(audienceMaterial, renderingLayerMask, worldBounds, color);
            }
        }

        private void DrawAudience(Material audienceMaterial, uint renderingLayerMask, Bounds worldBounds, FanlightColorSettings color)
        {
            Profiler.BeginSample("Prism Fanlight GPU Audience Draw");

            _audienceProperties ??= new MaterialPropertyBlock();
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceParts, _buffers.AudiencePartBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.VisibleIndexBuffer);
            // 観客からもペンライトと同じ per-seat カラーを参照できるようにバインドする
            // （Shader Graph の GetAudienceBodyColor_float 用）。
            _audienceProperties.SetBuffer(FanlightShaderIds.Colors, _buffers.ColorBuffer);
            _audienceProperties.SetInt(FanlightShaderIds.ColorSource, color.mode == FanlightColorMode.Single ? 0 : 1);
            _audienceProperties.SetColor(FanlightShaderIds.GlobalColor, color.GetGlobalColor());
            _audienceProperties.SetFloat(FanlightShaderIds.GlobalIntensity, color.GetGlobalIntensity());

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
            _instanceColorsInitialized = false;
            _lastInstanceColorHash = 0;
            _lastAnimationLocalToWorld = Matrix4x4.identity;
            _scheduler.Reset();
        }

        private static bool CanRender(Mesh mesh, Material material, ComputeShader computeShader, SeatLayout layout)
        {
            return mesh != null
                   && material != null
                   && computeShader != null
                   && layout.TotalSeatCount > 0
                   && layout.BlockSeatCount > 0;
        }

        private void EnsureInitialized(Mesh mesh, ComputeShader computeShader, SeatLayout layout, bool allocateAudience)
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
            _buffers.Allocate(mesh, layout, allocateAudience);
            _audienceAllocated = allocateAudience;
            _isInitialized = true;
        }

        private bool ShouldUpdateInstanceColors(FanlightColorSettings color)
        {
            if (color.mode == FanlightColorMode.Single)
            {
                return false;
            }

            return !_instanceColorsInitialized || _lastInstanceColorHash != color.GetStableHash();
        }
    }
}
