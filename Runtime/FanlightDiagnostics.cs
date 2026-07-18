namespace PrismFanlight
{
    public enum FanlightLayoutStatus
    {
        Legacy = 0,
        Ready = 1,
        BakeRequired = 2,
        Invalid = 3
    }

    public enum FanlightPenlightAppearanceStatus
    {
        MissingResource = 0,
        LegacySingleMesh = 1,
        Ready = 2,
        InvalidProfile = 3,
        StableSeatIdsRequired = 4
    }

    public readonly struct FanlightDiagnostics
    {
        public FanlightDiagnostics(
            bool isGpuReady,
            int totalSeatCount,
            int visibleSeatCount,
            int blockCount,
            FanlightLayoutStatus layoutStatus,
            int layoutBufferAllocationCount = 0,
            int partialLayoutUploadCount = 0,
            int lastLayoutUploadSeatCount = 0,
            FanlightPenlightAppearanceStatus appearanceStatus = FanlightPenlightAppearanceStatus.LegacySingleMesh,
            string appearanceProfileId = "",
            int appearanceProfileVersion = 0,
            int penlightVariantCount = 1,
            ulong penlightAssignmentHash = 0UL)
        {
            IsGpuReady = isGpuReady;
            TotalSeatCount = totalSeatCount;
            VisibleSeatCount = visibleSeatCount;
            BlockCount = blockCount;
            LayoutStatus = layoutStatus;
            LayoutBufferAllocationCount = layoutBufferAllocationCount;
            PartialLayoutUploadCount = partialLayoutUploadCount;
            LastLayoutUploadSeatCount = lastLayoutUploadSeatCount;
            AppearanceStatus = appearanceStatus;
            AppearanceProfileId = appearanceProfileId ?? string.Empty;
            AppearanceProfileVersion = appearanceProfileVersion;
            PenlightVariantCount = penlightVariantCount;
            PenlightAssignmentHash = penlightAssignmentHash;
        }


        public bool IsGpuReady { get; }

        public int TotalSeatCount { get; }

        public int VisibleSeatCount { get; }

        public int BlockCount { get; }

        public FanlightLayoutStatus LayoutStatus { get; }

        public int LayoutBufferAllocationCount { get; }

        public int PartialLayoutUploadCount { get; }

        public int LastLayoutUploadSeatCount { get; }

        public FanlightPenlightAppearanceStatus AppearanceStatus { get; }

        public string AppearanceProfileId { get; }

        public int AppearanceProfileVersion { get; }

        public int PenlightVariantCount { get; }

        public ulong PenlightAssignmentHash { get; }
    }
}
