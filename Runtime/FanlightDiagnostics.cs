namespace PrismFanlight
{
    public enum FanlightLayoutStatus
    {
        Legacy = 0,
        Ready = 1,
        BakeRequired = 2,
        Invalid = 3
    }

    public readonly struct FanlightDiagnostics
    {
        public FanlightDiagnostics(bool isGpuReady, int totalSeatCount, int visibleSeatCount, int blockCount)
            : this(isGpuReady, totalSeatCount, visibleSeatCount, blockCount, FanlightLayoutStatus.Legacy)
        {
        }

        public FanlightDiagnostics(
            bool isGpuReady,
            int totalSeatCount,
            int visibleSeatCount,
            int blockCount,
            FanlightLayoutStatus layoutStatus,
            int layoutBufferAllocationCount = 0,
            int partialLayoutUploadCount = 0,
            int lastLayoutUploadSeatCount = 0)
        {
            IsGpuReady = isGpuReady;
            TotalSeatCount = totalSeatCount;
            VisibleSeatCount = visibleSeatCount;
            BlockCount = blockCount;
            LayoutStatus = layoutStatus;
            LayoutBufferAllocationCount = layoutBufferAllocationCount;
            PartialLayoutUploadCount = partialLayoutUploadCount;
            LastLayoutUploadSeatCount = lastLayoutUploadSeatCount;
        }

        public bool IsGpuReady { get; }

        public int TotalSeatCount { get; }

        public int VisibleSeatCount { get; }

        public int BlockCount { get; }

        public FanlightLayoutStatus LayoutStatus { get; }

        public int LayoutBufferAllocationCount { get; }

        public int PartialLayoutUploadCount { get; }

        public int LastLayoutUploadSeatCount { get; }
    }
}
