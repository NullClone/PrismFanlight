using System;

namespace PrismFanlight.Rendering
{
    public readonly struct FanlightGpuTimingSummary
    {
        public double P50Milliseconds { get; }
        public double P95Milliseconds { get; }
        public double P99Milliseconds { get; }
        public double MaximumMilliseconds { get; }
        public int SampleCount { get; }

        public FanlightGpuTimingSummary(double p50Milliseconds, double p95Milliseconds, double p99Milliseconds, double maximumMilliseconds, int sampleCount)
        {
            P50Milliseconds = p50Milliseconds;
            P95Milliseconds = p95Milliseconds;
            P99Milliseconds = p99Milliseconds;
            MaximumMilliseconds = maximumMilliseconds;
            SampleCount = sampleCount;
        }
    }

    public readonly struct FanlightGpuBufferDiagnostic
    {
        public string BufferId { get; }
        public string OwnerId { get; }
        public string CameraId { get; }
        public int ElementCount { get; }
        public int StrideBytes { get; }
        public long CapacityBytes { get; }
        public string Lifetime { get; }

        public FanlightGpuBufferDiagnostic(string bufferId, string ownerId, string cameraId, int elementCount, int strideBytes, long capacityBytes, string lifetime)
        {
            BufferId = bufferId;
            OwnerId = ownerId;
            CameraId = cameraId;
            ElementCount = elementCount;
            StrideBytes = strideBytes;
            CapacityBytes = capacityBytes;
            Lifetime = lifetime;
        }
    }

    public readonly struct FanlightCameraDiagnostic
    {
        public string CameraId { get; }
        public int VisibleBlockCount { get; }
        public int VisibleSeatCount { get; }
        public int NearLodCount { get; }
        public int MidLodCount { get; }
        public int FarLodCount { get; }
        public long LastUsedFrame { get; }
        public bool IsCacheValid { get; }

        public FanlightCameraDiagnostic(string cameraId, int visibleBlockCount, int visibleSeatCount, int nearLodCount, int midLodCount, int farLodCount, long lastUsedFrame, bool isCacheValid)
        {
            CameraId = cameraId;
            VisibleBlockCount = visibleBlockCount;
            VisibleSeatCount = visibleSeatCount;
            NearLodCount = nearLodCount;
            MidLodCount = midLodCount;
            FarLodCount = farLodCount;
            LastUsedFrame = lastUsedFrame;
            IsCacheValid = isCacheValid;
        }
    }

    public readonly struct FanlightGpuDiagnostics
    {
        public string BackendId { get; }
        public FanlightRendererStatus Status { get; }
        public string LayoutId { get; }
        public int LayoutVersion { get; }
        public int SeatCount { get; }
        public int BlockCount { get; }
        public int ResidentCameraCount { get; }
        public long TotalBufferBytes { get; }
        public int DispatchCountThisFrame { get; }
        public int DrawCountThisFrame { get; }
        public long LastShowSampleSequence { get; }
        public double LastAnimationSampleSeconds { get; }
        public FanlightGpuTimingSummary Timing { get; }
        public long StaticUploadBytes { get; }
        public long DynamicUploadBytesThisFrame { get; }
        public long ReadbackRequestCount { get; }
        public long ResourceAllocationCount { get; }
        public string LastAllocationReason { get; }
        public bool HasStaleReadback { get; }
        public long LastSuccessfulReadbackFrame { get; }
        public ReadOnlyMemory<FanlightGpuBufferDiagnostic> Buffers { get; }
        public ReadOnlyMemory<FanlightCameraDiagnostic> Cameras { get; }

        public FanlightGpuDiagnostics(
            string backendId, FanlightRendererStatus status, string layoutId, int layoutVersion,
            int seatCount, int blockCount, int residentCameraCount, long totalBufferBytes,
            int dispatchCountThisFrame, int drawCountThisFrame, long lastShowSampleSequence,
            double lastAnimationSampleSeconds, FanlightGpuTimingSummary timing, long staticUploadBytes,
            long dynamicUploadBytesThisFrame, long readbackRequestCount, long resourceAllocationCount,
            string lastAllocationReason, bool hasStaleReadback, long lastSuccessfulReadbackFrame,
            ReadOnlyMemory<FanlightGpuBufferDiagnostic> buffers, ReadOnlyMemory<FanlightCameraDiagnostic> cameras)
        {
            BackendId = backendId;
            Status = status;
            LayoutId = layoutId;
            LayoutVersion = layoutVersion;
            SeatCount = seatCount;
            BlockCount = blockCount;
            ResidentCameraCount = residentCameraCount;
            TotalBufferBytes = totalBufferBytes;
            DispatchCountThisFrame = dispatchCountThisFrame;
            DrawCountThisFrame = drawCountThisFrame;
            LastShowSampleSequence = lastShowSampleSequence;
            LastAnimationSampleSeconds = lastAnimationSampleSeconds;
            Timing = timing;
            StaticUploadBytes = staticUploadBytes;
            DynamicUploadBytesThisFrame = dynamicUploadBytesThisFrame;
            ReadbackRequestCount = readbackRequestCount;
            ResourceAllocationCount = resourceAllocationCount;
            LastAllocationReason = lastAllocationReason;
            HasStaleReadback = hasStaleReadback;
            LastSuccessfulReadbackFrame = lastSuccessfulReadbackFrame;
            Buffers = buffers;
            Cameras = cameras;
        }
    }
}
