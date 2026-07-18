using System;
using System.Collections.Generic;
using System.Linq;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Profiling;

namespace PrismFanlight.Rendering
{
    public sealed class FanlightGpuRenderer : IFanlightRenderBackend
    {
        // Fields

        private readonly FanlightGpuBuffers _buffers = new();
        private readonly FanlightGpuDispatcher _dispatcher = new();
        private readonly FanlightGpuVisibilityReadback _visibilityReadback = new();
        private readonly FanlightGpuUpdateScheduler _scheduler = new();
        private readonly Vector4[] _paletteColors = new Vector4[FanlightColorSettings.PaletteSlotCount];
        private readonly Dictionary<string, FanlightCameraContext> _contractCameras = new(StringComparer.Ordinal);

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
        private int _layoutBufferAllocationCount;
        private int _partialLayoutUploadCount;
        private int _lastLayoutUploadSeatCount;
        private FanlightRendererStatus _contractStatus;
        private string _contractLayoutId = string.Empty;
        private int _contractLayoutVersion;
        private int _contractSeatCount;
        private int _contractBlockCount;
        private long _lastShowSampleSequence;
        private double _lastAnimationSampleSeconds;
        private int _drawCountThisFrame;
        private int _dispatchCountThisFrame;
        private long _lastPreparedFrame = -1;
        private bool _diagnosticReadbackRequested;


        // Properties

        public bool IsReady => _isInitialized;

        public int VisibleSeatCount => _visibilityReadback.VisibleSeatCount;

        public int LayoutBufferAllocationCount => _layoutBufferAllocationCount;

        public int PartialLayoutUploadCount => _partialLayoutUploadCount;

        public int LastLayoutUploadSeatCount => _lastLayoutUploadSeatCount;

        public FanlightPenlightAppearanceStatus AppearanceStatus { get; private set; } = FanlightPenlightAppearanceStatus.MissingResource;

        public string AppearanceProfileId => _appearance?.ProfileId ?? _resolvedAppearance?.ProfileId ?? string.Empty;

        public int AppearanceProfileVersion => _appearance?.ProfileVersion ?? _resolvedAppearance?.ProfileVersion ?? 0;

        public int PenlightVariantCount => _buffers.PenlightVariantCount;

        public ulong PenlightAssignmentHash => _buffers.PenlightAssignmentHash;

        public string BackendId => "legacy.matrix.compatibility";

        public FanlightRendererStatus Status => _isInitialized ? FanlightRendererStatus.Ready : _contractStatus;

        public FanlightRenderBackendCapabilities Capabilities => new(
            SystemInfo.supportsComputeShaders,
            true,
            SystemInfo.supportsAsyncGPUReadback,
            false,
            true,
            true,
            1);


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
            _drawCountThisFrame = 0;
            _dispatchCountThisFrame = 0;
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
                AppearanceStatus = FanlightPenlightAppearanceStatus.MissingResource;
                return;
            }

            if (!TryResolveAppearance(fallbackMesh, appearanceProfile, layout, out var appearance))
            {
                ReleaseResources();
                _contractStatus = FanlightRendererStatus.Faulted;
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
                Profiler.BeginSample("Prism Fanlight GPU Visibility");
                _dispatcher.DispatchVisibility(computeShader, _kernels, _buffers, context);
                _dispatchCountThisFrame += 3;
                if (_diagnosticReadbackRequested)
                {
                    _visibilityReadback.Request(_buffers.PenlightArgsBuffer, _buffers.SeatCount, _buffers.PenlightVariantCount);
                }

                _diagnosticReadbackRequested = false;
                Profiler.EndSample();
            }

            if (_scheduler.ShouldUpdateAnimation(animationUpdate, state.UpdateClock, refreshAllAnimation || visibilityUpdated))
            {
                Profiler.BeginSample("Prism Fanlight GPU Animation");
                _dispatcher.DispatchAnimation(computeShader, _kernels, _buffers, context, !refreshAllAnimation);
                _dispatchCountThisFrame++;
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
                _drawCountThisFrame++;
            }

            Profiler.EndSample();

            if (audienceEnabled)
            {
                var audienceBounds = worldBounds;
                audienceBounds.Expand(2.0f);
                DrawAudience(audienceMaterial, renderingLayerMask, audienceBounds, state.Color);
                _drawCountThisFrame++;
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
                    AppearanceStatus = FanlightPenlightAppearanceStatus.MissingResource;
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
                AppearanceStatus = FanlightPenlightAppearanceStatus.LegacySingleMesh;
                return appearance != null;
            }

            if (!profile.TryValidate(out _))
            {
                AppearanceStatus = FanlightPenlightAppearanceStatus.InvalidProfile;
                return false;
            }

            if (profile.VariantCount > 1 && !layout.HasStableSeatIds)
            {
                AppearanceStatus = FanlightPenlightAppearanceStatus.StableSeatIdsRequired;
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
            AppearanceStatus = appearance != null
                ? FanlightPenlightAppearanceStatus.Ready
                : FanlightPenlightAppearanceStatus.InvalidProfile;
            return appearance != null;
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
                    _lastLayoutUploadSeatCount = layout.SeatCount;
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
            _layoutBufferAllocationCount++;
            _lastLayoutUploadSeatCount = layout.SeatCount;
            _audienceAllocated = allocateAudience;
            _lastRandomHash = random.GetStableHash();
            _isInitialized = true;
            _contractStatus = FanlightRendererStatus.Ready;
        }

        public void LoadStaticData(
            FanlightLayoutRuntimeData layout,
            FanlightPersonaRuntimeData persona,
            FanlightGestureRuntimeData gestureLibrary)
        {
            if (string.IsNullOrWhiteSpace(layout.LayoutId))
            {
                throw new ArgumentException("LayoutId must be non-empty.", nameof(layout));
            }

            if (layout.LayoutVersion <= 0 || layout.BakeVersion <= 0 || layout.Seats.Length == 0 || layout.Blocks.Length == 0)
            {
                throw new ArgumentException("Layout versions, seats, and blocks must be present.", nameof(layout));
            }

            if (string.IsNullOrWhiteSpace(persona.PersonaProfileId) || persona.PersonaSchemaVersion <= 0)
            {
                throw new ArgumentException("Persona profile ID and schema version are required.", nameof(persona));
            }

            if (string.IsNullOrWhiteSpace(gestureLibrary.GestureLibraryId) || gestureLibrary.GestureLibraryVersion <= 0)
            {
                throw new ArgumentException("Gesture library ID and version are required.", nameof(gestureLibrary));
            }

            if (layout.Seats.Length != persona.PackedPersonas.Length
                && persona.Encoding == FanlightPersonaEncoding.Packed16Bytes)
            {
                throw new ArgumentException("Packed persona count must match seat count.", nameof(persona));
            }

            if (persona.Encoding == FanlightPersonaEncoding.IntegerHash && persona.PackedPersonas.Length != 0)
            {
                throw new ArgumentException("Integer-hash persona data must not include packed records.", nameof(persona));
            }

            ValidateStaticLayout(layout);

            _contractLayoutId = layout.LayoutId;
            _contractLayoutVersion = layout.LayoutVersion;
            _contractSeatCount = layout.Seats.Length;
            _contractBlockCount = layout.Blocks.Length;
            _contractStatus = FanlightRendererStatus.Degraded;
        }

        private static void ValidateStaticLayout(in FanlightLayoutRuntimeData layout)
        {
            var seatIds = new HashSet<ulong>();
            var seats = layout.Seats.Span;
            for (var i = 0; i < seats.Length; i++)
            {
                if (seats[i].StableSeatId == 0UL || !seatIds.Add(seats[i].StableSeatId))
                    throw new ArgumentException("Stable seat IDs must be non-zero and unique.", nameof(layout));
                if (seats[i].BlockIndex < 0 || seats[i].BlockIndex >= layout.Blocks.Length)
                    throw new ArgumentException("Seat block index is outside the block table.", nameof(layout));
            }

            var blocks = layout.Blocks.Span;
            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                var contiguous = block.ContiguousSeatCount > 0;
                var indexed = block.SeatIndexTableCount > 0;
                if (contiguous == indexed)
                    throw new ArgumentException("A block must use exactly one seat range representation.", nameof(layout));
                if (contiguous && (block.ContiguousSeatStart < 0 || (long)block.ContiguousSeatStart + block.ContiguousSeatCount > seats.Length))
                    throw new ArgumentException("Contiguous block seat range is invalid.", nameof(layout));
                if (indexed && (block.SeatIndexTableOffset < 0
                                || (long)block.SeatIndexTableOffset + block.SeatIndexTableCount > layout.BlockSeatIndexTable.Length))
                    throw new ArgumentException("Indexed block seat range is invalid.", nameof(layout));
            }
        }

        public void UnloadStaticData()
        {
            ReleaseResources();
            _contractLayoutId = string.Empty;
            _contractLayoutVersion = 0;
            _contractSeatCount = 0;
            _contractBlockCount = 0;
            _contractStatus = FanlightRendererStatus.Uninitialized;
        }

        public void ApplyShowSample(in FanlightShowSample sample)
        {
            if (!sample.IsComplete)
            {
                throw new ArgumentException("Renderer accepts only complete show samples.", nameof(sample));
            }

            _lastShowSampleSequence = sample.SampleSequence;
        }

        public void PrepareFrame(in FanlightFrameContext frame)
        {
            _lastPreparedFrame = frame.UnityFrameIndex;
            _lastAnimationSampleSeconds = frame.AnimationSampleSeconds;
            _drawCountThisFrame = 0;
            _dispatchCountThisFrame = 0;
        }

        public void RegisterCamera(in FanlightCameraContext camera)
        {
            ValidateCamera(camera);
            if (!_contractCameras.ContainsKey(camera.CameraId) && _contractCameras.Count >= Capabilities.MaximumResidentCameras)
            {
                _contractStatus = FanlightRendererStatus.Degraded;
                throw new InvalidOperationException("The compatibility backend supports one resident camera.");
            }

            _contractCameras[camera.CameraId] = camera;
        }

        public void UnregisterCamera(string cameraId)
        {
            if (!string.IsNullOrWhiteSpace(cameraId))
            {
                _contractCameras.Remove(cameraId);
            }
        }

        public void PrepareCamera(in FanlightCameraContext camera)
        {
            ValidateCamera(camera);
            if (!_contractCameras.ContainsKey(camera.CameraId))
            {
                RegisterCamera(camera);
            }
            else
            {
                _contractCameras[camera.CameraId] = camera;
            }
        }

        public void RenderCamera(in FanlightCameraContext camera)
        {
            ValidateCamera(camera);
            if (!_contractCameras.ContainsKey(camera.CameraId))
            {
                throw new InvalidOperationException("Camera must be registered before rendering.");
            }

            // Stage 1 keeps the matrix backend on its existing Render(...) entry point.
            // This method deliberately performs no second draw.
        }

        public FanlightGpuDiagnostics CaptureDiagnostics(bool requestReadback)
        {
            if (requestReadback) _diagnosticReadbackRequested = true;
            var diagnosticCameraId = _contractCameras.Count == 1
                ? Enumerable.First(_contractCameras.Keys)
                : string.Empty;
            var bufferDiagnostics = _buffers.CaptureDiagnostics(diagnosticCameraId);
            var cameraDiagnostics = new FanlightCameraDiagnostic[_contractCameras.Count];
            var index = 0;
            foreach (var pair in _contractCameras)
            {
                cameraDiagnostics[index++] = new FanlightCameraDiagnostic(
                    pair.Key,
                    0,
                    VisibleSeatCount,
                    0,
                    0,
                    0,
                    _lastPreparedFrame,
                    _isInitialized);
            }

            return new FanlightGpuDiagnostics(
                BackendId,
                Status,
                _contractLayoutId,
                _contractLayoutVersion,
                _contractSeatCount > 0 ? _contractSeatCount : _layout?.SeatCount ?? 0,
                _contractBlockCount > 0 ? _contractBlockCount : _layout?.BlockCount ?? 0,
                _contractCameras.Count,
                _buffers.TotalCapacityBytes,
                _dispatchCountThisFrame,
                _drawCountThisFrame,
                _lastShowSampleSequence,
                _lastAnimationSampleSeconds,
                default,
                _buffers.InitialStaticUploadBytes,
                0,
                _visibilityReadback.RequestCount,
                _layoutBufferAllocationCount,
                _layoutBufferAllocationCount > 0 ? "Legacy layout initialization" : string.Empty,
                _visibilityReadback.IsPending,
                _visibilityReadback.LastSuccessfulFrame,
                bufferDiagnostics,
                cameraDiagnostics,
                AppearanceProfileId,
                AppearanceProfileVersion,
                PenlightVariantCount,
                PenlightAssignmentHash,
                _buffers.PenlightVariantSeatCounts,
                _visibilityReadback.VisibleVariantCounts);
        }

        private static void ValidateCamera(in FanlightCameraContext camera)
        {
            if (string.IsNullOrWhiteSpace(camera.CameraId))
            {
                throw new ArgumentException("CameraId must be non-empty.", nameof(camera));
            }
        }

        internal bool ApplyEditorLayoutPreview(FanlightRuntimeLayout layout, int changedBlockIndex)
        {
            if (!_isInitialized || !layout.HasSameTopology(_layout)) return false;

            if (changedBlockIndex >= 0)
            {
                _buffers.UpdateBlock(_appearance, layout, changedBlockIndex);
                _partialLayoutUploadCount++;
                _lastLayoutUploadSeatCount = layout.Blocks[changedBlockIndex].count;
            }
            else
            {
                _buffers.UpdateStaticData(_appearance, layout);
                _lastLayoutUploadSeatCount = layout.SeatCount;
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
            _contractCameras.Clear();
            _contractStatus = FanlightRendererStatus.Disposed;
        }

        private void ReleaseResources()
        {
            _buffers.Release();
            _visibilityReadback.Reset();
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
