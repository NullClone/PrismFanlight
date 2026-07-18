using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuRenderer : IDisposable
    {
        // Fields

        private readonly FanlightGpuBuffers _buffers = new();
        private readonly FanlightGpuDispatcher _dispatcher = new();
        private readonly FanlightGpuUpdateScheduler _scheduler = new();
        private readonly Vector4[] _paletteColors = new Vector4[FanlightColorSettings.PaletteSlotCount];

        private MaterialPropertyBlock _properties;
        private MaterialPropertyBlock _audienceProperties;
        private FanlightGpuKernels _kernels;
        private FanlightRuntimeLayout _layout;
        private SeatLayout _legacySource;
        private FanlightRuntimeLayout _legacyRuntimeLayout;
        private int _legacyAuthoringHash;
        private FanlightPenlightRuntimeAppearance _appearance;
        private FanlightPenlightRuntimeAppearance _resolvedAppearance;
        private FanlightPenlightAppearanceProfile _resolvedAppearanceProfile;
        private Mesh _resolvedFallbackMesh;
        private ulong _resolvedAppearanceHash;
        private ComputeShader _computeShader;
        private bool _audienceAllocated;
        private bool _isInitialized;
        private bool _animationInitialized;
        private bool _hasLastUpdateClock;
        private int _lastRandomHash;
        private float _lastUpdateClock;
        private Matrix4x4 _lastAnimationLocalToWorld;


        // Properties

        internal bool IsReady => _isInitialized;


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
            var authoringHash = layout?.AuthoringHash ?? 0;
            if (_legacyRuntimeLayout == null || _legacySource != layout || _legacyAuthoringHash != authoringHash)
            {
                _legacySource = layout;
                _legacyAuthoringHash = authoringHash;
                _legacyRuntimeLayout = FanlightRuntimeLayout.FromLegacy(layout);
            }

            var runtimeLayout = _legacyRuntimeLayout;
            Render(
                mesh,
                null,
                material,
                computeShader,
                renderingLayerMask,
                cullingCamera,
                enableCulling,
                visibilityUpdate,
                animationUpdate,
                runtimeLayout,
                audienceMaterial,
                state,
                isTimeJump,
                lodCameraWorldPos);
            _legacySource = layout;
            _legacyAuthoringHash = authoringHash;
            _legacyRuntimeLayout = runtimeLayout;
        }

        internal void Render(
            Mesh fallbackMesh,
            FanlightPenlightAppearanceProfile appearanceProfile,
            Material material,
            ComputeShader computeShader,
            uint renderingLayerMask,
            Camera cullingCamera,
            bool enableCulling,
            FanlightGpuUpdateTiming visibilityUpdate,
            FanlightGpuUpdateTiming animationUpdate,
            FanlightRuntimeLayout layout,
            Material audienceMaterial,
            FanlightResolvedState state,
            bool isTimeJump,
            Vector3 lodCameraWorldPos)
        {
            if (material == null || computeShader == null || layout == null || !layout.HasValidTopology)
            {
                ReleaseResources();
                return;
            }

            if (!TryResolveAppearance(fallbackMesh, appearanceProfile, layout, out var appearance))
            {
                ReleaseResources();
                return;
            }

            var audienceEnabled = state.Audience.enabled && audienceMaterial != null;

            EnsureInitialized(appearance, computeShader, layout, audienceEnabled, state.Random);

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
                _dispatcher.DispatchVisibility(computeShader, _kernels, _buffers, context);
            }

            if (_scheduler.ShouldUpdateAnimation(animationUpdate, state.UpdateClock, refreshAllAnimation || visibilityUpdated))
            {
                _dispatcher.DispatchAnimation(computeShader, _kernels, _buffers, context, !refreshAllAnimation);
                _animationInitialized = true;
                _lastAnimationLocalToWorld = state.LocalToWorld;
            }

            _hasLastUpdateClock = true;
            _lastUpdateClock = state.UpdateClock;

            _properties.SetBuffer(FanlightShaderIds.Matrices, _buffers.MatrixBuffer);
            _properties.SetBuffer(FanlightShaderIds.ColorAssignments, _buffers.ColorAssignmentBuffer);
            _properties.SetBuffer(FanlightShaderIds.VisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            _properties.SetBuffer(FanlightShaderIds.PenlightVisibleIndices, _buffers.PenlightVisibleIndexBuffer);
            SetColorProperties(_properties, state.Color);

            for (var variantIndex = 0; variantIndex < appearance.VariantCount; variantIndex++)
            {
                _properties.SetInt(FanlightShaderIds.VisibleIndexBase, (int)_buffers.PenlightVariantOffsets[variantIndex]);
                var renderParams = new RenderParams(material)
                {
                    renderingLayerMask = renderingLayerMask,
                    receiveShadows = false,
                    worldBounds = worldBounds,
                    matProps = _properties
                };

                Graphics.RenderMeshIndirect(
                    renderParams,
                    appearance.Meshes[variantIndex],
                    _buffers.PenlightArgsBuffer,
                    1,
                    variantIndex);
            }

            if (audienceEnabled)
            {
                var audienceBounds = worldBounds;
                audienceBounds.Expand(2.0f);
                DrawAudience(audienceMaterial, renderingLayerMask, audienceBounds, state.Color);
            }
        }

        private bool TryResolveAppearance(
            Mesh fallbackMesh,
            FanlightPenlightAppearanceProfile profile,
            FanlightRuntimeLayout layout,
            out FanlightPenlightRuntimeAppearance appearance)
        {
            appearance = null;
            if (profile == null)
            {
                if (fallbackMesh == null)
                {
                    return false;
                }

                var hash = unchecked((ulong)(uint)fallbackMesh.GetInstanceID()) | 1UL;
                if (_resolvedAppearance == null || _resolvedAppearanceProfile != null
                                                || _resolvedFallbackMesh != fallbackMesh
                                                || _resolvedAppearanceHash != hash)
                {
                    _resolvedAppearance = FanlightPenlightRuntimeAppearance.CreateLegacy(fallbackMesh);
                    _resolvedAppearanceProfile = null;
                    _resolvedFallbackMesh = fallbackMesh;
                    _resolvedAppearanceHash = hash;
                }

                appearance = _resolvedAppearance;
                return appearance != null;
            }

            if (!profile.TryValidate(out _))
            {
                return false;
            }

            if (profile.VariantCount > 1 && !layout.HasStableSeatIds)
            {
                return false;
            }

            var contentHash = profile.GetRuntimeContentHash();
            if (_resolvedAppearance == null || _resolvedAppearanceProfile != profile || _resolvedAppearanceHash != contentHash)
            {
                _resolvedAppearance = FanlightPenlightRuntimeAppearance.Create(profile);
                _resolvedAppearanceProfile = profile;
                _resolvedFallbackMesh = null;
                _resolvedAppearanceHash = contentHash;
            }

            appearance = _resolvedAppearance;
            return appearance != null;
        }

        private void DrawAudience(Material audienceMaterial, uint renderingLayerMask, Bounds worldBounds, FanlightColorSettings color)
        {
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
        }

        private void EnsureInitialized(
            FanlightPenlightRuntimeAppearance appearance,
            ComputeShader computeShader,
            FanlightRuntimeLayout layout,
            bool allocateAudience,
            FanlightRandomSettings random)
        {
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

                return;
            }

            Dispose();

            _appearance = appearance;
            _computeShader = computeShader;
            _layout = layout;
            _kernels = new FanlightGpuKernels(computeShader);
            _properties = new MaterialPropertyBlock();
            _buffers.Allocate(appearance, layout, allocateAudience, random);
            _audienceAllocated = allocateAudience;
            _lastRandomHash = random.GetStableHash();
            _isInitialized = true;
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
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            _buffers.Release();
            _properties = null;
            _audienceProperties = null;
            _audienceAllocated = false;
            _appearance = null;
            _computeShader = null;
            _layout = null;
            _legacySource = null;
            _legacyRuntimeLayout = null;
            _legacyAuthoringHash = 0;
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
