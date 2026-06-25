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

        public ComputeBuffer AudiencePartBuffer { get; private set; }

        public GraphicsBuffer AudienceArgsBuffer { get; private set; }

        public bool HasAudience => AudiencePartBuffer != null;


        public int SeatCount { get; private set; }

        public int BlockCount { get; private set; }

        public Bounds LocalBounds { get; private set; }

        public float MeshPivotY { get; private set; }


        // Methods

        public void Allocate(Mesh mesh, SeatLayout layout, bool allocateAudience)
        {
            Release();

            SeatCount = layout.TotalSeatCount;
            BlockCount = layout.blockCount.x * layout.blockCount.y;
            LocalBounds = FanlightGeometryBuilder.BuildBounds(layout, mesh);
            MeshPivotY = mesh.bounds.min.y;

            SeatBuffer = new ComputeBuffer(SeatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            BlockBuffer = new ComputeBuffer(BlockCount, FanlightBlockData.Stride, ComputeBufferType.Structured);
            BlockVisibilityBuffer = new ComputeBuffer(BlockCount, sizeof(uint), ComputeBufferType.Structured);
            VisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            MatrixBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            ColorBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 4, ComputeBufferType.Structured);
            ArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);

            SeatBuffer.SetData(FanlightGeometryBuilder.BuildSeatData(layout));
            BlockBuffer.SetData(FanlightGeometryBuilder.BuildBlockData(layout, mesh));

            ResetArgs(ArgsBuffer, mesh);

            if (allocateAudience)
            {
                AudiencePartBuffer = new ComputeBuffer(SeatCount * FanlightAudiencePart.PartsPerSeat, FanlightAudiencePart.Stride, ComputeBufferType.Structured);
                AudienceArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);
                ResetArgs(AudienceArgsBuffer, FanlightGeometryBuilder.GetAudienceQuad());
            }
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

        public void Release()
        {
            SeatBuffer?.Release();
            BlockBuffer?.Release();
            BlockVisibilityBuffer?.Release();
            VisibleIndexBuffer?.Release();
            MatrixBuffer?.Release();
            ColorBuffer?.Release();
            ArgsBuffer?.Release();
            AudiencePartBuffer?.Release();
            AudienceArgsBuffer?.Release();

            SeatBuffer = null;
            BlockBuffer = null;
            BlockVisibilityBuffer = null;
            VisibleIndexBuffer = null;
            MatrixBuffer = null;
            ColorBuffer = null;
            ArgsBuffer = null;
            AudiencePartBuffer = null;
            AudienceArgsBuffer = null;
            SeatCount = 0;
            BlockCount = 0;
            LocalBounds = default;
            MeshPivotY = 0f;
        }
    }
}
