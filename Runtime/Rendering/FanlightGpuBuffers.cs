using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuBuffers
    {
        // Properties

        public ComputeBuffer SeatBuffer { get; private set; }

        public ComputeBuffer BlockBuffer { get; private set; }

        public ComputeBuffer BlockVisibilityBuffer { get; private set; }

        public ComputeBuffer VisibleIndexBuffer { get; private set; }

        public ComputeBuffer MatrixBuffer { get; private set; }

        public ComputeBuffer ColorBuffer { get; private set; }

        public GraphicsBuffer ArgsBuffer { get; private set; }


        public int SeatCount { get; private set; }

        public int BlockCount { get; private set; }

        public Bounds LocalBounds { get; private set; }


        // Methods

        public void Allocate(Mesh mesh, Audience audience)
        {
            Release();

            SeatCount = audience.TotalSeatCount;
            BlockCount = audience.blockCount.x * audience.blockCount.y;
            LocalBounds = FanlightGeometryBuilder.BuildBounds(audience, mesh);

            SeatBuffer = new ComputeBuffer(SeatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            BlockBuffer = new ComputeBuffer(BlockCount, FanlightBlockData.Stride, ComputeBufferType.Structured);
            BlockVisibilityBuffer = new ComputeBuffer(BlockCount, sizeof(uint), ComputeBufferType.Structured);
            VisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            MatrixBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            ColorBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 4, ComputeBufferType.Structured);
            ArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);

            SeatBuffer.SetData(FanlightGeometryBuilder.BuildSeatData(audience));
            BlockBuffer.SetData(FanlightGeometryBuilder.BuildBlockData(audience, mesh));

            ResetArgs(mesh);
        }

        private void ResetArgs(Mesh mesh)
        {
            ArgsBuffer.SetData(new[]
            {
                mesh.GetIndexCount(0),
                0u,
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0u
            });
        }

        public void Release()
        {
            SeatBuffer?.Release();
            BlockBuffer?.Release();
            BlockVisibilityBuffer?.Release();
            VisibleIndexBuffer?.Release();
            MatrixBuffer?.Release();
            ColorBuffer?.Release();
            ArgsBuffer?.Release();

            SeatBuffer = null;
            BlockBuffer = null;
            BlockVisibilityBuffer = null;
            VisibleIndexBuffer = null;
            MatrixBuffer = null;
            ColorBuffer = null;
            ArgsBuffer = null;
            SeatCount = 0;
            BlockCount = 0;
            LocalBounds = default;
        }

        public long EstimateMemoryBytes()
        {
            if (SeatBuffer == null) return 0;

            return (long)SeatCount * FanlightSeatData.Stride
                   + (long)BlockCount * FanlightBlockData.Stride
                   + (long)BlockCount * sizeof(uint)
                   + (long)SeatCount * sizeof(uint)
                   + (long)SeatCount * sizeof(float) * 16
                   + (long)SeatCount * sizeof(float) * 4
                   + sizeof(uint) * 5;
        }
    }
}
