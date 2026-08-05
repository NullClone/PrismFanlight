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

        private readonly FanlightGpuBuffers _buffers = new();
        private readonly FanlightGpuDispatcher _dispatcher = new();
        private readonly FanlightGpuUpdateScheduler _scheduler = new();

        private MaterialPropertyBlock _properties;
        private MaterialPropertyBlock _audienceProperties;
        private FanlightGpuKernels _kernels;
        private FanlightRuntimeLayout _layout;
        private FanlightPenlightRuntimeAppearance _appearance;
        private FanlightPenlightAsset _penlightAsset;
        private Material _penlightMaterial;
        private Material _audienceMaterial;
        private ComputeShader _computeShader;
        private Mesh _audienceMesh;
        private bool _audienceAllocated;
        private bool _isInitialized;
        private bool _animationInitialized;
        private bool _colorInitialized;
        private bool _maskInitialized;
        private bool _hasGlobalSeed;
        private bool _hasMaskBeat;
        private bool _hasVisibilityContext;
        private bool _lastCullingEnabled;
        private uint _globalSeed;
        private double _lastMaskBeat;
        private Camera _lastCullingCamera;
        private Matrix4x4 _lastCullingViewMatrix;
        private Matrix4x4 _lastCullingProjectionMatrix;
        private Matrix4x4 _lastAnimationLocalToWorld;
        private Matrix4x4 _lastVisibilityLocalToWorld;
        private FanlightColorState _lastColorState;
        private FanlightIntensityState _lastIntensityState;


        // Properties

        internal bool IsReady => _isInitialized && Fault == FanlightRendererFault.None;

        internal FanlightRendererFault Fault { get; private set; }


        // Methods

        internal void Load(
            FanlightRuntimeLayout layout,
            FanlightPenlightAsset penlightAsset,
            Material penlightMaterial,
            Material audienceMaterial,
            ComputeShader computeShader)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                FailLoad(FanlightRendererFault.UnsupportedComputeShader);
                return;
            }

            if (layout == null)
            {
                FailLoad(FanlightRendererFault.InvalidLayout);
                return;
            }

            if (penlightAsset == null || penlightMaterial == null || computeShader == null)
            {
                FailLoad(FanlightRendererFault.MissingResource);
                return;
            }

            var appearanceHash = penlightAsset.GetRuntimeContentHash();
            var allocateAudience = audienceMaterial != null;

            if (_isInitialized
                && ReferenceEquals(_layout, layout)
                && _appearance != null
                && _appearance.ContentHash == appearanceHash
                && _penlightAsset == penlightAsset
                && _penlightMaterial == penlightMaterial
                && _audienceMaterial == audienceMaterial
                && _computeShader == computeShader
                && _audienceAllocated == allocateAudience)
            {
                Fault = FanlightRendererFault.None;
                return;
            }

            if (!layout.HasValidTopology
                || !penlightAsset.TryValidate(out _)
                || penlightAsset.VariantCount > 1 && !layout.HasStableSeatIds)
            {
                FailLoad(FanlightRendererFault.InvalidLayout);
                return;
            }

            var appearance = _appearance;
            if (appearance == null || _penlightAsset != penlightAsset || appearance.ContentHash != appearanceHash)
            {
                appearance = FanlightPenlightRuntimeAppearance.Create(penlightAsset);
            }

            if (appearance == null)
            {
                FailLoad(FanlightRendererFault.MissingResource);
                return;
            }

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
                    _animationInitialized = false;
                    _colorInitialized = false;
                    _maskInitialized = false;
                    _hasVisibilityContext = false;
                    _scheduler.Reset();
                }

                _layout = layout;
                _appearance = appearance;
                _penlightAsset = penlightAsset;
                _penlightMaterial = penlightMaterial;
                _audienceMaterial = audienceMaterial;
                Fault = FanlightRendererFault.None;
                return;
            }

            ReleaseResources();

            _appearance = appearance;
            _penlightAsset = penlightAsset;
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
            Camera cullingCamera,
            bool cullingEnabled,
            int layer,
            uint renderingLayerMask,
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
                _colorInitialized = false;
            }

            if (_buffers.HasMotionAssetChanges(sample.State.Motion))
            {
                _animationInitialized = false;
            }

            var worldBounds = FanlightGeometryBuilder.TransformBounds(frame.LocalToWorld, _buffers.LocalBounds);
            var context = new FanlightGpuDispatchContext(_layout, sample, frame);

            try
            {
                if (!_colorInitialized || !_lastColorState.ContentEquals(sample.State.Color))
                {
                    _dispatcher.DispatchColor(_computeShader, _kernels, _buffers, context);
                    _lastColorState = sample.State.Color;
                    _colorInitialized = true;
                }

                var intensity = sample.State.Intensity;
                var completedBeat = sample.MusicalPosition.Beat;
                var maskInputsChanged = !_maskInitialized
                                        || !_lastIntensityState.MaskContentEquals(intensity);
                var maskBeatChanged = intensity.HasDynamicMask()
                                      && (!_hasMaskBeat || !_lastMaskBeat.Equals(completedBeat));

                if (maskInputsChanged || maskBeatChanged)
                {
                    _dispatcher.DispatchMask(_computeShader, _kernels, _buffers, context);
                    _lastIntensityState = intensity;
                    _lastMaskBeat = completedBeat;
                    _hasMaskBeat = true;
                    _maskInitialized = true;
                }
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

            if (sample.Discontinuity != FanlightTimeDiscontinuity.None)
            {
                _scheduler.Reset();
                _animationInitialized = false;
            }

            var refreshAllAnimation = !_animationInitialized || frame.LocalToWorld != _lastAnimationLocalToWorld;
            var visibilityContextChanged = !_hasVisibilityContext
                                           || HasVisibilityContextChanged(cullingCamera, cullingEnabled);
            var visibilityTransformChanged = cullingEnabled
                                             && (!_hasVisibilityContext
                                                 || frame.LocalToWorld != _lastVisibilityLocalToWorld);
            var forceVisibility = visibilityContextChanged || visibilityTransformChanged;
            var visibilityUpdated = cullingEnabled
                ? _scheduler.ShouldUpdateVisibility(
                    visibilityTiming,
                    (float)sample.ShowSeconds,
                    forceVisibility)
                : forceVisibility;

            if (visibilityUpdated)
            {
                _dispatcher.DispatchVisibility(
                    _computeShader,
                    _kernels,
                    _buffers,
                    context,
                    cullingCamera,
                    cullingEnabled);
                _lastCullingEnabled = cullingEnabled;
                _lastCullingCamera = cullingCamera;
                _lastCullingViewMatrix = cullingEnabled ? cullingCamera.worldToCameraMatrix : default;
                _lastCullingProjectionMatrix = cullingEnabled ? cullingCamera.projectionMatrix : default;
                _lastVisibilityLocalToWorld = frame.LocalToWorld;
                _hasVisibilityContext = true;
            }

            if (_scheduler.ShouldUpdateAnimation(
                    animationTiming,
                    (float)sample.AnimationSampleSeconds,
                    refreshAllAnimation))
            {
                _buffers.UpdateMotionData(sample.State.Motion);
                _dispatcher.DispatchAnimation(_computeShader, _kernels, _buffers, context);
                _animationInitialized = true;
                _lastAnimationLocalToWorld = frame.LocalToWorld;
            }

            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            SetEmissionProperties(_properties, sample.State.Intensity);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            _properties.SetBuffer(FanlightShaderIds.PenlightVisibleIndices, _buffers.PenlightVisibleIndexBuffer);

            if (sample.State.Visibility.PenlightsEnabled)
            {
                DrawPenlights(layer, renderingLayerMask, worldBounds);
            }

            if (sample.State.Visibility.AudienceBodiesEnabled && _audienceMaterial != null)
            {
                var audienceBounds = worldBounds;
                audienceBounds.Expand(2f);
                DrawAudience(layer, renderingLayerMask, audienceBounds, sample.State.Intensity);
            }
        }

        internal bool ApplyEditorLayoutPreview(FanlightRuntimeLayout layout, int changedBlockIndex)
        {
            if (!_isInitialized
                || layout == null
                || !layout.HasValidTopology
                || !layout.HasSameTopology(_layout)
                || layout.StableSeatIdHash != _layout.StableSeatIdHash)
            {
                return false;
            }

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
            _colorInitialized = false;
            _maskInitialized = false;
            _hasVisibilityContext = false;
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
                || double.IsInfinity(sample.AnimationSampleSeconds)
                || !sample.MusicalPosition.IsComplete)
            {
                throw new ArgumentException("A complete show sample is required.", nameof(sample));
            }

            _ = FanlightShowStatePatcher.Validate(sample.State);
            if (!sample.State.Motion.HasValidAssets())
            {
                throw new InvalidOperationException("A complete show sample requires valid baked Motion Assets.");
            }
        }

        private bool HasVisibilityContextChanged(Camera cullingCamera, bool cullingEnabled)
        {
            if (cullingEnabled != _lastCullingEnabled) return true;
            if (!cullingEnabled) return false;

            return cullingCamera != _lastCullingCamera
                   || cullingCamera.worldToCameraMatrix != _lastCullingViewMatrix
                   || cullingCamera.projectionMatrix != _lastCullingProjectionMatrix;
        }

        private void DrawPenlights(int layer, uint renderingLayerMask, Bounds worldBounds)
        {
            for (var variantIndex = 0; variantIndex < _appearance.VariantCount; variantIndex++)
            {
                _properties.SetInt(FanlightShaderIds.VisibleIndexBase, (int)_buffers.PenlightVariantOffsets[variantIndex]);
                var renderParams = new RenderParams(_penlightMaterial)
                {
                    camera = null, // すべてのカメラに対して描画
                    layer = layer,
                    renderingLayerMask = renderingLayerMask,
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
            int layer,
            uint renderingLayerMask,
            Bounds worldBounds,
            FanlightIntensityState intensity)
        {
            _audienceProperties ??= new MaterialPropertyBlock();
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceParts, _buffers.AudiencePartBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.AudienceVisibleIndexBuffer);
            _audienceProperties.SetBuffer(FanlightShaderIds.AudienceVisibleIndices, _buffers.AudienceVisibleIndexBuffer);
            SetEmissionProperties(_audienceProperties, intensity);

            var renderParams = new RenderParams(_audienceMaterial)
            {
                camera = null, // すべてのカメラに対して描画
                layer = layer,
                renderingLayerMask = renderingLayerMask,
                receiveShadows = false,
                worldBounds = worldBounds,
                matProps = _audienceProperties
            };

            Graphics.RenderMeshIndirect(renderParams, _audienceMesh, _buffers.AudienceArgsBuffer);
        }

        private void SetEmissionProperties(
            MaterialPropertyBlock properties,
            FanlightIntensityState intensity)
        {
            properties.SetBuffer(FanlightShaderIds.StableAssignments, _buffers.StableAssignmentBuffer);
            properties.SetBuffer(FanlightShaderIds.ResolvedChroma, _buffers.ResolvedChromaBuffer);
            properties.SetBuffer(FanlightShaderIds.ResolvedMask, _buffers.ResolvedMaskBuffer);
            properties.SetFloat(FanlightShaderIds.BaseIntensity, intensity.BaseIntensity);
            properties.SetFloat(FanlightShaderIds.RandomIntensity, intensity.RandomIntensity);
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
            _penlightAsset = null;
            _penlightMaterial = null;
            _audienceMaterial = null;
            _computeShader = null;
            _audienceMesh = null;
            _audienceAllocated = false;
            _isInitialized = false;
            _animationInitialized = false;
            _colorInitialized = false;
            _maskInitialized = false;
            _hasGlobalSeed = false;
            _hasMaskBeat = false;
            _hasVisibilityContext = false;
            _lastCullingEnabled = false;
            _globalSeed = 0u;
            _lastMaskBeat = 0d;
            _lastCullingCamera = null;
            _lastCullingViewMatrix = Matrix4x4.identity;
            _lastCullingProjectionMatrix = Matrix4x4.identity;
            _lastAnimationLocalToWorld = Matrix4x4.identity;
            _lastVisibilityLocalToWorld = Matrix4x4.identity;
            _lastColorState = default;
            _lastIntensityState = default;
            _scheduler.Reset();
        }
    }
}
