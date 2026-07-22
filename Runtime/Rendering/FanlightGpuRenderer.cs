using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuRenderer : IDisposable
    {
        // Fields

        private const int PaletteSlotCount = 6;

        private readonly FanlightGpuBuffers _buffers = new();
        private readonly FanlightGpuDispatcher _dispatcher = new();
        private readonly FanlightGpuUpdateScheduler _scheduler = new();
        private readonly Vector4[] _paletteColors = new Vector4[PaletteSlotCount];

        private MaterialPropertyBlock _properties;
        private MaterialPropertyBlock _audienceProperties;
        private FanlightGpuKernels _kernels;
        private FanlightRuntimeLayout _layout;
        private FanlightPenlightRuntimeAppearance _appearance;
        private FanlightPenlightAppearanceProfile _appearanceProfile;
        private Material _penlightMaterial;
        private Material _audienceMaterial;
        private ComputeShader _computeShader;
        private Mesh _audienceMesh;
        private bool _audienceAllocated;
        private bool _isInitialized;
        private bool _animationInitialized;
        private bool _hasGlobalSeed;
        private bool _hasCameraContext;
        private uint _globalSeed;
        private FanlightCameraContext _lastCameraContext;
        private Matrix4x4 _lastAnimationLocalToWorld;


        // Properties

        internal bool IsReady => _isInitialized && Fault == FanlightRendererFault.None;

        internal FanlightRendererFault Fault { get; private set; }


        // Methods

        internal void Load(
            FanlightRuntimeLayout layout,
            FanlightPenlightAppearanceProfile appearanceProfile,
            Material penlightMaterial,
            Material audienceMaterial,
            ComputeShader computeShader)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                FailLoad(FanlightRendererFault.UnsupportedComputeShader);
                return;
            }

            if (layout == null || !layout.HasValidTopology)
            {
                FailLoad(FanlightRendererFault.InvalidLayout);
                return;
            }

            if (appearanceProfile == null || penlightMaterial == null || computeShader == null)
            {
                FailLoad(FanlightRendererFault.MissingResource);
                return;
            }

            if (!appearanceProfile.TryValidate(out _)
                || appearanceProfile.VariantCount > 1 && !layout.HasStableSeatIds)
            {
                FailLoad(FanlightRendererFault.InvalidLayout);
                return;
            }

            var appearanceHash = appearanceProfile.GetRuntimeContentHash();
            var appearance = _appearance;
            if (appearance == null || _appearanceProfile != appearanceProfile || appearance.ContentHash != appearanceHash)
            {
                appearance = FanlightPenlightRuntimeAppearance.Create(appearanceProfile);
            }

            if (appearance == null)
            {
                FailLoad(FanlightRendererFault.MissingResource);
                return;
            }

            var allocateAudience = audienceMaterial != null;
            if (_isInitialized
                && _appearance != null
                && _appearance.ContentHash == appearance.ContentHash
                && _computeShader == computeShader
                && _audienceAllocated == allocateAudience
                && layout.HasSameTopology(_layout)
                && layout.StableSeatIdHash == _layout.StableSeatIdHash)
            {
                if (_layout.ContentHash != layout.ContentHash)
                {
                    _buffers.UpdateStaticData(appearance, layout);
                    _layout = layout;
                    _animationInitialized = false;
                    _scheduler.Reset();
                }

                _appearance = appearance;
                _appearanceProfile = appearanceProfile;
                _penlightMaterial = penlightMaterial;
                _audienceMaterial = audienceMaterial;
                Fault = FanlightRendererFault.None;
                return;
            }

            ReleaseResources();

            _appearance = appearance;
            _appearanceProfile = appearanceProfile;
            _penlightMaterial = penlightMaterial;
            _audienceMaterial = audienceMaterial;
            _computeShader = computeShader;
            _layout = layout;

            try
            {
                _kernels = new FanlightGpuKernels(computeShader);
            }
            catch (ArgumentException)
            {
                FailLoad(FanlightRendererFault.UnsupportedComputeShader);
                return;
            }

            _properties = new MaterialPropertyBlock();
            _audienceMesh = allocateAudience ? FanlightGeometryBuilder.CreateAudienceQuad() : null;

            try
            {
                _buffers.Allocate(appearance, layout, allocateAudience, _audienceMesh, 0u);
            }
            catch (Exception)
            {
                FailLoad(FanlightRendererFault.MissingResource);
                return;
            }

            _audienceAllocated = allocateAudience;
            _isInitialized = true;
            Fault = FanlightRendererFault.None;
        }

        internal void Render(
            in FanlightShowSample sample,
            in FanlightFrameContext frame,
            in FanlightCameraContext camera,
            in FanlightGpuUpdateTiming visibilityTiming,
            in FanlightGpuUpdateTiming animationTiming)
        {
            if (!_isInitialized) return;

            try
            {
                ValidateSample(sample);
            }
            catch (ArgumentException)
            {
                Fault = FanlightRendererFault.InvalidShowSample;
                return;
            }
            catch (InvalidOperationException)
            {
                Fault = FanlightRendererFault.InvalidShowSample;
                return;
            }

            Fault = FanlightRendererFault.None;

            if (!_hasGlobalSeed || _globalSeed != sample.State.GlobalSeed)
            {
                _buffers.UpdateRandomData(sample.State.GlobalSeed, _layout);
                _globalSeed = sample.State.GlobalSeed;
                _hasGlobalSeed = true;
                _animationInitialized = false;
            }

            if (_buffers.UpdateMotionData(sample.State.Motion))
            {
                _animationInitialized = false;
            }

            var worldBounds = FanlightGeometryBuilder.TransformBounds(frame.LocalToWorld, _buffers.LocalBounds);
            var context = new FanlightGpuDispatchContext(_layout, sample, frame, camera, worldBounds);
            var discontinuity = sample.Discontinuity != FanlightTimeDiscontinuity.None;
            if (discontinuity)
            {
                _scheduler.Reset();
                _animationInitialized = false;
            }

            var refreshAllAnimation = !_animationInitialized || frame.LocalToWorld != _lastAnimationLocalToWorld;
            var cameraChanged = !_hasCameraContext || HasCameraChanged(camera, _lastCameraContext);
            var visibilityUpdated = refreshAllAnimation
                                    || cameraChanged
                                    || _scheduler.ShouldUpdateVisibility(visibilityTiming, (float)sample.ShowSeconds);

            if (visibilityUpdated)
            {
                _dispatcher.DispatchVisibility(_computeShader, _kernels, _buffers, context);
            }

            if (_scheduler.ShouldUpdateAnimation(
                    animationTiming,
                    (float)sample.AnimationSampleSeconds,
                    refreshAllAnimation))
            {
                _dispatcher.DispatchAnimation(_computeShader, _kernels, _buffers, context, !refreshAllAnimation);
                _animationInitialized = true;
                _lastAnimationLocalToWorld = frame.LocalToWorld;
            }

            _lastCameraContext = camera;
            _hasCameraContext = true;

            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            _properties.SetBuffer(FanlightShaderIds.ColorAssignments, _buffers.ColorAssignmentBuffer);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            _properties.SetBuffer(FanlightShaderIds.PenlightVisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            SetColorProperties(_properties, sample.State.Palette);

            if (sample.State.Visibility.PenlightsEnabled)
            {
                DrawPenlights(camera, worldBounds);
            }

            if (sample.State.Visibility.AudienceBodiesEnabled && _audienceMaterial != null)
            {
                var audienceBounds = worldBounds;
                audienceBounds.Expand(2f);
                DrawAudience(camera, audienceBounds, sample.State.Palette);
            }
        }

        internal bool ApplyEditorLayoutPreview(FanlightRuntimeLayout layout, int changedBlockIndex)
        {
            if (!_isInitialized || !layout.HasSameTopology(_layout)) return false;

            if (changedBlockIndex >= 0)
            {
                _buffers.UpdateBlock(_appearance, layout, changedBlockIndex);
            }
            else
            {
                _buffers.UpdateStaticData(_appearance, layout);
            }

            _layout = layout;
            _animationInitialized = false;
            _scheduler.Reset();
            return true;
        }

        public void Dispose()
        {
            ReleaseResources();
            Fault = FanlightRendererFault.None;
        }

        private static void ValidateSample(in FanlightShowSample sample)
        {
            if (double.IsNaN(sample.ShowSeconds)
                || double.IsInfinity(sample.ShowSeconds)
                || double.IsNaN(sample.AnimationSampleSeconds)
                || double.IsInfinity(sample.AnimationSampleSeconds))
            {
                throw new ArgumentException("A complete show sample is required.", nameof(sample));
            }

            _ = FanlightShowStatePatcher.Validate(sample.State);
            if (!sample.State.Motion.HasValidAssets())
            {
                throw new InvalidOperationException("A complete show sample requires valid baked Motion Assets.");
            }
        }

        private static bool HasCameraChanged(
            in FanlightCameraContext current,
            in FanlightCameraContext previous)
        {
            return !string.Equals(current.CameraId, previous.CameraId, StringComparison.Ordinal)
                   || current.Camera != previous.Camera
                   || current.ViewMatrix != previous.ViewMatrix
                   || current.ProjectionMatrix != previous.ProjectionMatrix
                   || current.WorldPosition != previous.WorldPosition
                   || current.CullingEnabled != previous.CullingEnabled;
        }

        private void DrawPenlights(in FanlightCameraContext camera, Bounds worldBounds)
        {
            for (var variantIndex = 0; variantIndex < _appearance.VariantCount; variantIndex++)
            {
                _properties.SetInt(FanlightShaderIds.VisibleIndexBase, (int)_buffers.PenlightVariantOffsets[variantIndex]);
                var renderParams = new RenderParams(_penlightMaterial)
                {
                    camera = camera.Camera,
                    renderingLayerMask = camera.RenderingLayerMask,
                    receiveShadows = false,
                    worldBounds = worldBounds,
                    matProps = _properties
                };

                Graphics.RenderMeshIndirect(
                    renderParams,
                    _appearance.Meshes[variantIndex],
                    _buffers.PenlightArgsBuffer,
                    1,
                    variantIndex);
            }
        }

        private void DrawAudience(
            in FanlightCameraContext camera,
            Bounds worldBounds,
            FanlightPaletteState palette)
        {
            _audienceProperties ??= new MaterialPropertyBlock();
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceParts, _buffers.AudiencePartBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.AudienceVisibleIndexBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceVisibleIndices, _buffers.AudienceVisibleIndexBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.ColorAssignments, _buffers.ColorAssignmentBuffer);
            SetColorProperties(_audienceProperties, palette);

            var renderParams = new RenderParams(_audienceMaterial)
            {
                camera = camera.Camera,
                renderingLayerMask = camera.RenderingLayerMask,
                receiveShadows = false,
                worldBounds = worldBounds,
                matProps = _audienceProperties
            };

            Graphics.RenderMeshIndirect(renderParams, _audienceMesh, _buffers.AudienceArgsBuffer);
        }

        private void SetColorProperties(MaterialPropertyBlock properties, FanlightPaletteState palette)
        {
            _paletteColors[0] = palette.Slot1;
            _paletteColors[1] = palette.Slot2;
            _paletteColors[2] = palette.Slot3;
            _paletteColors[3] = palette.Slot4;
            _paletteColors[4] = palette.Slot5;
            _paletteColors[5] = palette.Slot6;

            properties.SetVectorArray(FanlightShaderIds.PaletteColors, _paletteColors);
            properties.SetFloat(FanlightShaderIds.GlobalIntensity, palette.GlobalIntensity);
            properties.SetFloat(FanlightShaderIds.RandomIntensity, palette.RandomIntensity);
        }

        private void FailLoad(FanlightRendererFault fault)
        {
            ReleaseResources();
            Fault = fault;
        }

        private void ReleaseResources()
        {
            _buffers.Release();
            if (_audienceMesh != null)
            {
                if (Application.isPlaying) Object.Destroy(_audienceMesh);
                else Object.DestroyImmediate(_audienceMesh);
            }

            _properties = null;
            _audienceProperties = null;
            _kernels = default;
            _layout = null;
            _appearance = null;
            _appearanceProfile = null;
            _penlightMaterial = null;
            _audienceMaterial = null;
            _computeShader = null;
            _audienceMesh = null;
            _audienceAllocated = false;
            _isInitialized = false;
            _animationInitialized = false;
            _hasGlobalSeed = false;
            _hasCameraContext = false;
            _globalSeed = 0u;
            _lastCameraContext = default;
            _lastAnimationLocalToWorld = Matrix4x4.identity;
            _scheduler.Reset();
        }
    }
}
