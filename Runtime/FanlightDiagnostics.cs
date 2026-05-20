namespace PrismFanlight
{
    public readonly struct FanlightDiagnostics
    {
        public FanlightDiagnostics(bool isGpuReady, int totalSeatCount, int visibleSeatCount, int blockCount)
        {
            IsGpuReady = isGpuReady;
            TotalSeatCount = totalSeatCount;
            VisibleSeatCount = visibleSeatCount;
            BlockCount = blockCount;
        }

        public bool IsGpuReady { get; }

        public int TotalSeatCount { get; }

        public int VisibleSeatCount { get; }

        public int BlockCount { get; }
    }
}
