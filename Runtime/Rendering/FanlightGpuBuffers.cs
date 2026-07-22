using System;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuBuffers
    {
        // Fields

        private const int PaletteSlotCount = 6;

        private readonly FanlightBlockData[] _singleBlockUpload = new FanlightBlockData[1];


        // Properties

        internal ComputeBuffer SeatBuffer { get; private set; }

        internal ComputeBuffer BlockBuffer { get; private set; }

        internal ComputeBuffer BlockVisibilityBuffer { get; private set; }

        internal ComputeBuffer PenlightVisibleIndexBuffer { get; private set; }

        internal ComputeBuffer PenlightVariantAssignmentBuffer { get; private set; }

        internal ComputeBuffer PenlightVariantOffsetBuffer { get; private set; }

        internal ComputeBuffer AudienceVisibleIndexBuffer { get; private set; }

        internal ComputeBuffer AudienceSlotBuffer { get; private set; }

        internal ComputeBuffer MatrixBuffer { get; private set; }

        internal ComputeBuffer ColorAssignmentBuffer { get; private set; }

        internal ComputeBuffer RandomBuffer { get; private set; }

        internal GraphicsBuffer PenlightArgsBuffer { get; private set; }

        internal ComputeBuffer AudiencePartBuffer { get; private set; }

        internal GraphicsBuffer AudienceArgsBuffer { get; private set; }

        internal bool HasAudience => AudiencePartBuffer != null;

        internal int SeatCount { get; private set; }

        internal int BlockCount { get; private set; }

        internal Bounds LocalBounds { get; private set; }

        internal int PenlightVariantCount { get; private set; }

        internal uint[] PenlightVariantOffsets { get; private set; } = Array.Empty<uint>();

        internal Vector4 PenlightVariantGripPivotYs { get; private set; }


        // Methods

        internal void Allocate(
            FanlightPenlightRuntimeAppearance appearance,
            FanlightRuntimeLayout layout,
            bool allocateAudience,
            Mesh audienceMesh,
            uint globalSeed)
        {
            Release();

            SeatCount = layout.SeatCount;
            BlockCount = layout.BlockCount;
            PenlightVariantCount = appearance.VariantCount;
            LocalBounds = ExpandBounds(layout.LocalBounds, appearance.BoundsPadding);
            PenlightVariantGripPivotYs = BuildGripPivotVector(appearance.GripPivotYs);

            var assignments = BuildVariantAssignments(layout, appearance, out var counts);
            PenlightVariantOffsets = BuildVariantOffsets(counts);

            SeatBuffer = new ComputeBuffer(SeatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            BlockBuffer = new ComputeBuffer(BlockCount, FanlightBlockData.Stride, ComputeBufferType.Structured);
            BlockVisibilityBuffer = new ComputeBuffer(BlockCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVariantAssignmentBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVariantOffsetBuffer = new ComputeBuffer(PenlightVariantCount, sizeof(uint), ComputeBufferType.Structured);
            AudienceVisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            AudienceSlotBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            MatrixBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            ColorAssignmentBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            RandomBuffer = new ComputeBuffer(SeatCount, FanlightRandomData.Stride, ComputeBufferType.Structured);
            PenlightArgsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                PenlightVariantCount,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
            AudienceArgsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);

            SeatBuffer.SetData(layout.Seats);
            BlockBuffer.SetData(BuildBlockData(layout, appearance.BoundsPadding));
            PenlightVariantAssignmentBuffer.SetData(assignments);
            PenlightVariantOffsetBuffer.SetData(PenlightVariantOffsets);
            UpdateRandomData(globalSeed, layout);
            ResetPenlightArgs(PenlightArgsBuffer, appearance.Meshes);
            ResetArgs(AudienceArgsBuffer, audienceMesh);

            if (allocateAudience)
            {
                AudiencePartBuffer = new ComputeBuffer(SeatCount * FanlightAudiencePart.PartsPerSeat, FanlightAudiencePart.Stride, ComputeBufferType.Structured);
            }
        }

        internal void UpdateStaticData(FanlightPenlightRuntimeAppearance appearance, FanlightRuntimeLayout layout)
        {
            if (SeatBuffer == null || BlockBuffer == null || layout.SeatCount != SeatCount || layout.BlockCount != BlockCount)
            {
                throw new InvalidOperationException("Static layout topology does not match allocated GPU buffers.");
            }

            SeatBuffer.SetData(layout.Seats);
            BlockBuffer.SetData(BuildBlockData(layout, appearance.BoundsPadding));
            LocalBounds = ExpandBounds(layout.LocalBounds, appearance.BoundsPadding);
        }

        internal void UpdateBlock(FanlightPenlightRuntimeAppearance appearance, FanlightRuntimeLayout layout, int blockIndex)
        {
            if (SeatBuffer == null || BlockBuffer == null || blockIndex < 0 || blockIndex >= layout.BlockCount) return;

            var block = layout.Blocks[blockIndex];
            if (block.count > 0)
            {
                SeatBuffer.SetData(layout.Seats, block.startIndex, block.startIndex, block.count);
            }

            _singleBlockUpload[0] = ToBlockData(block, appearance.BoundsPadding);
            BlockBuffer.SetData(_singleBlockUpload, 0, blockIndex, 1);
            LocalBounds = ExpandBounds(layout.LocalBounds, appearance.BoundsPadding);
        }

        private static FanlightBlockData[] BuildBlockData(FanlightRuntimeLayout layout, float boundsPadding)
        {
            var data = new FanlightBlockData[layout.BlockCount];
            for (var i = 0; i < data.Length; i++) data[i] = ToBlockData(layout.Blocks[i], boundsPadding);
            return data;
        }

        private static FanlightBlockData ToBlockData(FanlightBakedBlockData block, float boundsPadding)
        {
            return new FanlightBlockData(block.localCenter, block.radius + boundsPadding, block.startIndex, block.count);
        }

        private static Bounds ExpandBounds(Bounds bounds, float boundsPadding)
        {
            bounds.Expand(boundsPadding * 2f);
            return bounds;
        }

        internal void UpdateRandomData(uint globalSeed, FanlightRuntimeLayout layout)
        {
            if (RandomBuffer == null
                || ColorAssignmentBuffer == null
                || layout == null
                || layout.SeatCount != SeatCount
                || SeatCount <= 0)
            {
                return;
            }

            RandomBuffer.SetData(BuildRandomData(layout, globalSeed));
            ColorAssignmentBuffer.SetData(BuildColorAssignments(layout, globalSeed));
        }

        private static void ResetArgs(GraphicsBuffer argsBuffer, Mesh mesh)
        {
            argsBuffer.SetData(new[]
            {
                new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = mesh != null ? mesh.GetIndexCount(0) : 0u,
                    instanceCount = 0u,
                    startIndex = mesh != null ? mesh.GetIndexStart(0) : 0u,
                    baseVertexIndex = mesh != null ? mesh.GetBaseVertex(0) : 0,
                    startInstance = 0u
                }
            });
        }

        private static void ResetPenlightArgs(GraphicsBuffer argsBuffer, Mesh[] meshes)
        {
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[meshes.Length];
            for (var i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                args[i] = new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = mesh.GetIndexCount(0),
                    instanceCount = 0u,
                    startIndex = mesh.GetIndexStart(0),
                    baseVertexIndex = mesh.GetBaseVertex(0),
                    startInstance = 0u
                };
            }

            argsBuffer.SetData(args);
        }

        private static uint[] BuildVariantAssignments(FanlightRuntimeLayout layout, FanlightPenlightRuntimeAppearance appearance, out int[] counts)
        {
            var assignments = new uint[layout.SeatCount];
            counts = new int[appearance.VariantCount];

            for (var i = 0; i < assignments.Length; i++)
            {
                var variantIndex = 0;
                var stableSeatId = layout.StableSeatIds[i];
                if (appearance.VariantCount > 1)
                {
                    variantIndex = FanlightPenlightAssignment.SelectVariantIndex(
                        stableSeatId,
                        appearance.AssignmentSeed,
                        FanlightPenlightAssignment.PersonaAlgorithmVersion,
                        appearance.StableVariantIds);
                }

                assignments[i] = (uint)variantIndex;
                counts[variantIndex]++;
            }

            return assignments;
        }

        private static uint[] BuildVariantOffsets(int[] counts)
        {
            var offsets = new uint[counts.Length];
            var offset = 0u;
            for (var i = 0; i < counts.Length; i++)
            {
                offsets[i] = offset;
                offset += (uint)counts[i];
            }

            return offsets;
        }

        private static Vector4 BuildGripPivotVector(float[] pivots)
        {
            return new Vector4(
                pivots.Length > 0 ? pivots[0] : 0f,
                pivots.Length > 1 ? pivots[1] : 0f,
                pivots.Length > 2 ? pivots[2] : 0f,
                pivots.Length > 3 ? pivots[3] : 0f);
        }

        private static FanlightRandomData[] BuildRandomData(FanlightRuntimeLayout layout, uint seed)
        {
            var data = new FanlightRandomData[layout.SeatCount];

            for (var i = 0; i < data.Length; i++)
            {
                var stableSeatId = layout.StableSeatIds[i];
                data[i] = new FanlightRandomData
                {
                    random0 = Random4(seed, stableSeatId, 0u),
                    random1 = Random4(seed, stableSeatId, 4u),
                    random2 = Random4(seed, stableSeatId, 8u),
                    random3 = Random4(seed, stableSeatId, 12u),
                    random4 = Random4(seed, stableSeatId, 16u),
                    random5 = Random4(seed, stableSeatId, 20u),
                    random6 = Random4(seed, stableSeatId, 24u),
                    random7 = Random4(seed, stableSeatId, 28u)
                };
            }

            return data;
        }

        private static uint[] BuildColorAssignments(FanlightRuntimeLayout layout, uint seed)
        {
            var assignments = new uint[layout.SeatCount];
            for (var i = 0; i < assignments.Length; i++)
            {
                var stableSeatId = layout.StableSeatIds[i];
                var paletteRandom = Random01(seed, stableSeatId, 27u);
                var intensityRandom = Random01(seed, stableSeatId, 28u);
                var paletteIndex = (uint)Mathf.Clamp(
                    Mathf.FloorToInt(paletteRandom * PaletteSlotCount),
                    0,
                    PaletteSlotCount - 1);
                var packedIntensity = (uint)Mathf.Clamp(Mathf.FloorToInt(intensityRandom * 65536.0f), 0, 65535);
                assignments[i] = paletteIndex | (packedIntensity << 8);
            }

            return assignments;
        }

        private static Vector4 Random4(uint globalSeed, ulong stableSeatId, uint offset)
        {
            return new Vector4(
                Random01(globalSeed, stableSeatId, offset + 0u),
                Random01(globalSeed, stableSeatId, offset + 1u),
                Random01(globalSeed, stableSeatId, offset + 2u),
                Random01(globalSeed, stableSeatId, offset + 3u));
        }

        private static float Random01(uint globalSeed, ulong stableSeatId, uint lane)
        {
            var x = globalSeed ^ 0x9E3779B9u;
            x ^= (uint)stableSeatId + 0x85EBCA6Bu + (x << 6) + (x >> 2);
            x ^= (uint)(stableSeatId >> 32) + 0x27D4EB2Fu + (x << 6) + (x >> 2);
            x ^= lane + 0xC2B2AE35u + (x << 6) + (x >> 2);
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;
            return (x & 0x00FFFFFFu) / 16777215.0f;
        }

        internal void Release()
        {
            SeatBuffer?.Release();
            BlockBuffer?.Release();
            BlockVisibilityBuffer?.Release();
            PenlightVisibleIndexBuffer?.Release();
            PenlightVariantAssignmentBuffer?.Release();
            PenlightVariantOffsetBuffer?.Release();
            AudienceVisibleIndexBuffer?.Release();
            AudienceSlotBuffer?.Release();
            MatrixBuffer?.Release();
            ColorAssignmentBuffer?.Release();
            RandomBuffer?.Release();
            PenlightArgsBuffer?.Release();
            AudiencePartBuffer?.Release();
            AudienceArgsBuffer?.Release();

            SeatBuffer = null;
            BlockBuffer = null;
            BlockVisibilityBuffer = null;
            PenlightVisibleIndexBuffer = null;
            PenlightVariantAssignmentBuffer = null;
            PenlightVariantOffsetBuffer = null;
            AudienceVisibleIndexBuffer = null;
            AudienceSlotBuffer = null;
            MatrixBuffer = null;
            ColorAssignmentBuffer = null;
            RandomBuffer = null;
            PenlightArgsBuffer = null;
            AudiencePartBuffer = null;
            AudienceArgsBuffer = null;
            SeatCount = 0;
            BlockCount = 0;
            LocalBounds = default;
            PenlightVariantCount = 0;
            PenlightVariantOffsets = Array.Empty<uint>();
            PenlightVariantGripPivotYs = default;
        }
    }
}
