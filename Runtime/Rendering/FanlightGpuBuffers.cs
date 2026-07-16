using System;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuBuffers
    {
        private readonly FanlightBlockData[] _singleBlockUpload = new FanlightBlockData[1];

        // Properties

        public ComputeBuffer SeatBuffer { get; private set; }

        public ComputeBuffer BlockBuffer { get; private set; }

        public ComputeBuffer BlockVisibilityBuffer { get; private set; }

        public ComputeBuffer PenlightVisibleIndexBuffer { get; private set; }

        public ComputeBuffer AudienceVisibleIndexBuffer { get; private set; }

        public ComputeBuffer AudienceSlotBuffer { get; private set; }

        public ComputeBuffer MatrixBuffer { get; private set; }

        public ComputeBuffer ColorAssignmentBuffer { get; private set; }

        public ComputeBuffer RandomBuffer { get; private set; }

        public GraphicsBuffer PenlightArgsBuffer { get; private set; }

        public ComputeBuffer AudiencePartBuffer { get; private set; }

        public GraphicsBuffer AudienceArgsBuffer { get; private set; }

        public bool HasAudience => AudiencePartBuffer != null;

        public int SeatCount { get; private set; }

        public int BlockCount { get; private set; }

        public Bounds LocalBounds { get; private set; }

        public float MeshPivotY { get; private set; }


        // Methods

        public void Allocate(Mesh mesh, FanlightRuntimeLayout layout, bool allocateAudience, FanlightRandomSettings random)
        {
            Release();

            SeatCount = layout.SeatCount;
            BlockCount = layout.BlockCount;
            LocalBounds = ExpandBounds(layout.LocalBounds, mesh);
            MeshPivotY = mesh.bounds.min.y;

            SeatBuffer = new ComputeBuffer(SeatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            BlockBuffer = new ComputeBuffer(BlockCount, FanlightBlockData.Stride, ComputeBufferType.Structured);
            BlockVisibilityBuffer = new ComputeBuffer(BlockCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            AudienceVisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            AudienceSlotBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            MatrixBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            ColorAssignmentBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            RandomBuffer = new ComputeBuffer(SeatCount, FanlightRandomData.Stride, ComputeBufferType.Structured);
            PenlightArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);
            AudienceArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);

            SeatBuffer.SetData(layout.Seats);
            BlockBuffer.SetData(BuildBlockData(layout, mesh));
            UpdateRandomData(random);

            ResetArgs(PenlightArgsBuffer, mesh);
            ResetArgs(AudienceArgsBuffer, FanlightGeometryBuilder.GetAudienceQuad());

            if (allocateAudience)
            {
                AudiencePartBuffer = new ComputeBuffer(SeatCount * FanlightAudiencePart.PartsPerSeat, FanlightAudiencePart.Stride, ComputeBufferType.Structured);
            }
        }

        public void UpdateStaticData(Mesh mesh, FanlightRuntimeLayout layout)
        {
            if (SeatBuffer == null || BlockBuffer == null
                                   || layout.SeatCount != SeatCount
                                   || layout.BlockCount != BlockCount)
            {
                throw new InvalidOperationException("Static layout topology does not match allocated GPU buffers.");
            }

            SeatBuffer.SetData(layout.Seats);
            BlockBuffer.SetData(BuildBlockData(layout, mesh));
            LocalBounds = ExpandBounds(layout.LocalBounds, mesh);
        }

        public void UpdateBlock(Mesh mesh, FanlightRuntimeLayout layout, int blockIndex)
        {
            if (SeatBuffer == null || BlockBuffer == null || blockIndex < 0 || blockIndex >= layout.BlockCount) return;

            var block = layout.Blocks[blockIndex];
            if (block.count > 0)
            {
                SeatBuffer.SetData(layout.Seats, block.startIndex, block.startIndex, block.count);
            }

            _singleBlockUpload[0] = ToBlockData(block, mesh);
            BlockBuffer.SetData(_singleBlockUpload, 0, blockIndex, 1);
            LocalBounds = ExpandBounds(layout.LocalBounds, mesh);
        }

        private static FanlightBlockData[] BuildBlockData(FanlightRuntimeLayout layout, Mesh mesh)
        {
            var data = new FanlightBlockData[layout.BlockCount];
            for (var i = 0; i < data.Length; i++) data[i] = ToBlockData(layout.Blocks[i], mesh);
            return data;
        }

        private static FanlightBlockData ToBlockData(FanlightBakedBlockData block, Mesh mesh)
        {
            var meshPadding = mesh.bounds.size.magnitude + 4.0f;
            return new FanlightBlockData(block.localCenter, block.radius + meshPadding, block.startIndex, block.count);
        }

        private static Bounds ExpandBounds(Bounds bounds, Mesh mesh)
        {
            bounds.Expand(mesh.bounds.size.magnitude + 4.0f);
            return bounds;
        }

        public void UpdateRandomData(FanlightRandomSettings random)
        {
            if (RandomBuffer == null || ColorAssignmentBuffer == null || SeatCount <= 0) return;

            var seed = random.deterministic ? random.globalSeed : (uint)Environment.TickCount;
            RandomBuffer.SetData(BuildRandomData(SeatCount, seed));
            ColorAssignmentBuffer.SetData(BuildColorAssignments(SeatCount, seed));
        }

        private static void ResetArgs(GraphicsBuffer argsBuffer, Mesh mesh)
        {
            argsBuffer.SetData(new[]
            {
                mesh.GetIndexCount(0),
                0u,
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0u
            });
        }

        private static FanlightRandomData[] BuildRandomData(int seatCount, uint seed)
        {
            var data = new FanlightRandomData[seatCount];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new FanlightRandomData
                {
                    random0 = Random4(seed, (uint)i, 0u),
                    random1 = Random4(seed, (uint)i, 4u),
                    random2 = Random4(seed, (uint)i, 8u),
                    random3 = Random4(seed, (uint)i, 12u),
                    random4 = Random4(seed, (uint)i, 16u),
                    random5 = Random4(seed, (uint)i, 20u),
                    random6 = Random4(seed, (uint)i, 24u),
                    random7 = Random4(seed, (uint)i, 28u)
                };
            }

            return data;
        }

        private static uint[] BuildColorAssignments(int seatCount, uint seed)
        {
            var assignments = new uint[seatCount];
            for (var i = 0; i < assignments.Length; i++)
            {
                var paletteRandom = Random01(seed, (uint)i, 27u);
                var intensityRandom = Random01(seed, (uint)i, 28u);
                var paletteIndex = (uint)Mathf.Clamp(
                    Mathf.FloorToInt(paletteRandom * FanlightColorSettings.PaletteSlotCount),
                    0,
                    FanlightColorSettings.PaletteSlotCount - 1);
                var packedIntensity = (uint)Mathf.Clamp(Mathf.FloorToInt(intensityRandom * 65536.0f), 0, 65535);
                assignments[i] = paletteIndex | (packedIntensity << 8);
            }

            return assignments;
        }

        private static Vector4 Random4(uint globalSeed, uint seatIndex, uint offset)
        {
            return new Vector4(
                Random01(globalSeed, seatIndex, offset + 0u),
                Random01(globalSeed, seatIndex, offset + 1u),
                Random01(globalSeed, seatIndex, offset + 2u),
                Random01(globalSeed, seatIndex, offset + 3u));
        }

        private static float Random01(uint globalSeed, uint seatIndex, uint lane)
        {
            var x = globalSeed ^ 0x9E3779B9u;
            x ^= seatIndex + 0x85EBCA6Bu + (x << 6) + (x >> 2);
            x ^= lane + 0xC2B2AE35u + (x << 6) + (x >> 2);
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;
            return (x & 0x00FFFFFFu) / 16777215.0f;
        }

        public void Release()
        {
            SeatBuffer?.Release();
            BlockBuffer?.Release();
            BlockVisibilityBuffer?.Release();
            PenlightVisibleIndexBuffer?.Release();
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
            MeshPivotY = 0f;
        }
    }
}
