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
        private MaterialPropertyBlock _bodyProperties;
        private FanlightGpuKernels _kernels;
        private Audience _audience;
        private Mesh _mesh;
        private ComputeShader _computeShader;
        private bool _bodyAllocated;
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
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            FanlightBodySettings body,
            Material audienceMaterial,
            Vector3 swingTargetWorldPos,
            Matrix4x4 localToWorld,
            float time,
            float updateClock)
        {
            var validatedAudience = (audience ?? Audience.Default()).Validated();

            if (!CanRender(mesh, material, computeShader, validatedAudience))
            {
                Dispose();
                return;
            }

            var bodyEnabled = body.enabled && audienceMaterial != null;
            var handBaseHeight = bodyEnabled ? body.bodyHeight * body.shoulderHeight : 0f;

            EnsureInitialized(mesh, computeShader, validatedAudience, bodyEnabled);

            var worldBounds = FanlightGeometryBuilder.TransformBounds(localToWorld, _buffers.LocalBounds);

            var context = new FanlightGpuDispatchContext(
                cullingCamera,
                enableCulling,
                validatedAudience,
                tempo,
                motion,
                color,
                body,
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

                if (bodyEnabled)
                {
                    _dispatcher.DispatchBodyArgs(computeShader, _kernels, _buffers);
                }

                Profiler.EndSample();
            }

            var refreshAllAnimation = !_animationInitialized || localToWorld != _lastAnimationLocalToWorld;

            if (_scheduler.ShouldUpdateAnimation(animationUpdate, updateClock, refreshAllAnimation))
            {
                Profiler.BeginSample("Prism Fanlight GPU Animation");
                _dispatcher.DispatchAnimation(computeShader, _kernels, _buffers, context, !refreshAllAnimation);

                if (bodyEnabled)
                {
                    _dispatcher.DispatchBody(computeShader, _kernels, _buffers, context, !refreshAllAnimation);
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

            if (bodyEnabled)
            {
                DrawBody(audienceMaterial, renderingLayerMask, worldBounds, color);
            }
        }

        private void DrawBody(Material bodyMaterial, uint renderingLayerMask, Bounds worldBounds, FanlightColorSettings color)
        {
            Profiler.BeginSample("Prism Fanlight GPU Body Draw");

            _bodyProperties ??= new MaterialPropertyBlock();
            _bodyProperties.SetBuffer(FanlightShaderIds.BodyParts, _buffers.BodyPartBuffer);
            _bodyProperties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.VisibleIndexBuffer);
            _bodyProperties.SetBuffer(FanlightShaderIds.Colors, _buffers.ColorBuffer);
            _bodyProperties.SetInt(FanlightShaderIds.ColorSource, color.mode == FanlightColorMode.Single ? 0 : 1);
            _bodyProperties.SetColor(FanlightShaderIds.GlobalColor, color.GetGlobalColor());
            _bodyProperties.SetFloat(FanlightShaderIds.GlobalIntensity, color.GetGlobalIntensity());

            var renderParams = new RenderParams(bodyMaterial)
            {
                renderingLayerMask = renderingLayerMask,
                receiveShadows = false,
                worldBounds = worldBounds,
                matProps = _bodyProperties
            };

            Graphics.RenderMeshIndirect(renderParams, FanlightGeometryBuilder.GetBodyQuad(), _buffers.BodyArgsBuffer);
            Profiler.EndSample();
        }

        public void Dispose()
        {
            _buffers.Release();
            _visibilityReadback.Reset();
            _properties = null;
            _bodyProperties = null;
            _bodyAllocated = false;
            _mesh = null;
            _computeShader = null;
            _isInitialized = false;
            _animationInitialized = false;
            _instanceColorsInitialized = false;
            _lastInstanceColorHash = 0;
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

        private void EnsureInitialized(Mesh mesh, ComputeShader computeShader, Audience audience, bool allocateBody)
        {
            if (_isInitialized
                && _mesh == mesh
                && _computeShader == computeShader
                && _bodyAllocated == allocateBody
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
            _buffers.Allocate(mesh, audience, allocateBody);
            _bodyAllocated = allocateBody;
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
