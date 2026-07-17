using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    public enum FanlightPersonaEncoding
    {
        IntegerHash = 0,
        Packed16Bytes = 1
    }

    public enum FanlightGestureRuntimeKind
    {
        StandardAnalytic = 0,
        SampledCurve = 1
    }

    public readonly struct FanlightSeatRuntimeData
    {
        public ulong StableSeatId { get; }
        public Vector3 LocalPosition { get; }
        public int BlockIndex { get; }
        public uint PlacementFlags { get; }

        public FanlightSeatRuntimeData(ulong stableSeatId, Vector3 localPosition, int blockIndex, uint placementFlags)
        {
            StableSeatId = stableSeatId;
            LocalPosition = localPosition;
            BlockIndex = blockIndex;
            PlacementFlags = placementFlags;
        }
    }

    public readonly struct FanlightBlockRuntimeData
    {
        public Bounds LocalBounds { get; }
        public int ContiguousSeatStart { get; }
        public int ContiguousSeatCount { get; }
        public int SeatIndexTableOffset { get; }
        public int SeatIndexTableCount { get; }

        public FanlightBlockRuntimeData(Bounds localBounds, int contiguousSeatStart, int contiguousSeatCount, int seatIndexTableOffset, int seatIndexTableCount)
        {
            LocalBounds = localBounds;
            ContiguousSeatStart = contiguousSeatStart;
            ContiguousSeatCount = contiguousSeatCount;
            SeatIndexTableOffset = seatIndexTableOffset;
            SeatIndexTableCount = seatIndexTableCount;
        }
    }

    public readonly struct FanlightPackedPersona
    {
        public uint Word0 { get; }
        public uint Word1 { get; }
        public uint Word2 { get; }
        public uint Word3 { get; }

        public FanlightPackedPersona(uint word0, uint word1, uint word2, uint word3)
        {
            Word0 = word0;
            Word1 = word1;
            Word2 = word2;
            Word3 = word3;
        }
    }

    public readonly struct FanlightLayoutRuntimeData
    {
        public string LayoutId { get; }
        public int LayoutVersion { get; }
        public int BakeVersion { get; }
        public ReadOnlyMemory<FanlightSeatRuntimeData> Seats { get; }
        public ReadOnlyMemory<FanlightBlockRuntimeData> Blocks { get; }
        public ReadOnlyMemory<int> BlockSeatIndexTable { get; }

        public FanlightLayoutRuntimeData(string layoutId, int layoutVersion, int bakeVersion, ReadOnlyMemory<FanlightSeatRuntimeData> seats, ReadOnlyMemory<FanlightBlockRuntimeData> blocks, ReadOnlyMemory<int> blockSeatIndexTable)
        {
            LayoutId = layoutId;
            LayoutVersion = layoutVersion;
            BakeVersion = bakeVersion;
            Seats = seats;
            Blocks = blocks;
            BlockSeatIndexTable = blockSeatIndexTable;
        }
    }

    public readonly struct FanlightPersonaRuntimeData
    {
        public string PersonaProfileId { get; }
        public int PersonaSchemaVersion { get; }
        public uint GlobalSeed { get; }
        public FanlightPersonaEncoding Encoding { get; }
        public ReadOnlyMemory<FanlightPackedPersona> PackedPersonas { get; }

        public FanlightPersonaRuntimeData(string personaProfileId, int personaSchemaVersion, uint globalSeed, FanlightPersonaEncoding encoding, ReadOnlyMemory<FanlightPackedPersona> packedPersonas)
        {
            PersonaProfileId = personaProfileId;
            PersonaSchemaVersion = personaSchemaVersion;
            GlobalSeed = globalSeed;
            Encoding = encoding;
            PackedPersonas = packedPersonas;
        }
    }

    public readonly struct FanlightGestureCurveSample
    {
        public float Phase { get; }
        public float PrimaryAxisValue { get; }
        public float SecondaryAxisValue { get; }
        public float ReachOffset { get; }
        public float BodyAccent { get; }

        public FanlightGestureCurveSample(float phase, float primaryAxisValue, float secondaryAxisValue, float reachOffset, float bodyAccent)
        {
            Phase = phase;
            PrimaryAxisValue = primaryAxisValue;
            SecondaryAxisValue = secondaryAxisValue;
            ReachOffset = reachOffset;
            BodyAccent = bodyAccent;
        }
    }

    public readonly struct FanlightGestureRuntimeDefinition
    {
        public string GestureId { get; }
        public uint StableNumericId { get; }
        public FanlightGestureRuntimeKind RuntimeKind { get; }
        public uint StandardKernelId { get; }
        public int CurveSampleOffset { get; }
        public int CurveSampleCount { get; }
        public FanlightExpertPatch DefaultExpert { get; }

        public FanlightGestureRuntimeDefinition(string gestureId, uint stableNumericId, FanlightGestureRuntimeKind runtimeKind, uint standardKernelId, int curveSampleOffset, int curveSampleCount, FanlightExpertPatch defaultExpert)
        {
            GestureId = gestureId;
            StableNumericId = stableNumericId;
            RuntimeKind = runtimeKind;
            StandardKernelId = standardKernelId;
            CurveSampleOffset = curveSampleOffset;
            CurveSampleCount = curveSampleCount;
            DefaultExpert = defaultExpert;
        }
    }

    public readonly struct FanlightGestureRuntimeData
    {
        public string GestureLibraryId { get; }
        public int GestureLibraryVersion { get; }
        public ReadOnlyMemory<FanlightGestureRuntimeDefinition> Definitions { get; }
        public ReadOnlyMemory<FanlightGestureCurveSample> CurveSamples { get; }

        public FanlightGestureRuntimeData(string gestureLibraryId, int gestureLibraryVersion, ReadOnlyMemory<FanlightGestureRuntimeDefinition> definitions, ReadOnlyMemory<FanlightGestureCurveSample> curveSamples)
        {
            GestureLibraryId = gestureLibraryId;
            GestureLibraryVersion = gestureLibraryVersion;
            Definitions = definitions;
            CurveSamples = curveSamples;
        }
    }

    public enum FanlightRendererStatus
    {
        Uninitialized = 0,
        Ready = 1,
        Degraded = 2,
        Faulted = 3,
        Disposed = 4
    }

    public readonly struct FanlightRenderBackendCapabilities
    {
        public bool SupportsComputeShaders { get; }
        public bool SupportsIndirectDraw { get; }
        public bool SupportsAsyncReadback { get; }
        public bool SupportsMultipleCameras { get; }
        public bool SupportsAudienceBodies { get; }
        public bool SupportsDistanceLod { get; }
        public int MaximumResidentCameras { get; }

        public FanlightRenderBackendCapabilities(bool supportsComputeShaders, bool supportsIndirectDraw, bool supportsAsyncReadback, bool supportsMultipleCameras, bool supportsAudienceBodies, bool supportsDistanceLod, int maximumResidentCameras)
        {
            SupportsComputeShaders = supportsComputeShaders;
            SupportsIndirectDraw = supportsIndirectDraw;
            SupportsAsyncReadback = supportsAsyncReadback;
            SupportsMultipleCameras = supportsMultipleCameras;
            SupportsAudienceBodies = supportsAudienceBodies;
            SupportsDistanceLod = supportsDistanceLod;
            MaximumResidentCameras = maximumResidentCameras;
        }
    }

    public readonly struct FanlightFrameContext
    {
        public long UnityFrameIndex { get; }
        public double ShowSeconds { get; }
        public double AnimationSampleSeconds { get; }
        public bool IsDiscontinuous { get; }

        public FanlightFrameContext(long unityFrameIndex, double showSeconds, double animationSampleSeconds, bool isDiscontinuous)
        {
            UnityFrameIndex = unityFrameIndex;
            ShowSeconds = showSeconds;
            AnimationSampleSeconds = animationSampleSeconds;
            IsDiscontinuous = isDiscontinuous;
        }
    }

    public readonly struct FanlightCameraContext
    {
        public string CameraId { get; }
        public Camera Camera { get; }
        public Matrix4x4 ViewMatrix { get; }
        public Matrix4x4 ProjectionMatrix { get; }
        public Vector3 WorldPosition { get; }
        public uint RenderingLayerMask { get; }
        public bool EnableCulling { get; }
        public bool EnableLod { get; }

        public FanlightCameraContext(string cameraId, Camera camera, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Vector3 worldPosition, uint renderingLayerMask, bool enableCulling, bool enableLod)
        {
            CameraId = cameraId;
            Camera = camera;
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
            WorldPosition = worldPosition;
            RenderingLayerMask = renderingLayerMask;
            EnableCulling = enableCulling;
            EnableLod = enableLod;
        }
    }

    public interface IFanlightRenderBackend : IDisposable
    {
        string BackendId { get; }
        FanlightRendererStatus Status { get; }
        FanlightRenderBackendCapabilities Capabilities { get; }
        void LoadStaticData(FanlightLayoutRuntimeData layout, FanlightPersonaRuntimeData persona, FanlightGestureRuntimeData gestureLibrary);
        void UnloadStaticData();
        void ApplyShowSample(in FanlightShowSample sample);
        void PrepareFrame(in FanlightFrameContext frame);
        void RegisterCamera(in FanlightCameraContext camera);
        void UnregisterCamera(string cameraId);
        void PrepareCamera(in FanlightCameraContext camera);
        void RenderCamera(in FanlightCameraContext camera);
        FanlightGpuDiagnostics CaptureDiagnostics(bool requestReadback);
    }
}
