using System;
using Unity.Mathematics;

namespace PrismFanlight
{
    [Serializable]
    public struct Audience
    {
        // Fields

        public int2 seatPerBlock;
        public float2 seatPitch;
        public int2 blockCount;
        public float2 aisleWidth;


        // Properties

        public static Audience Default()
            => new()
            {
                seatPerBlock = math.int2(8, 12),
                seatPitch = math.float2(0.4f, 0.8f),
                blockCount = math.int2(7, 3),
                aisleWidth = math.float2(0.7f, 1.2f)
            };

        public int BlockSeatCount => seatPerBlock.x * seatPerBlock.y;

        public int TotalSeatCount => seatPerBlock.x * seatPerBlock.y * blockCount.x * blockCount.y;


        // Methods

        public Audience Validated() => new()
        {
            seatPerBlock = math.max(seatPerBlock, math.int2(1, 1)),
            seatPitch = math.max(seatPitch, math.float2(0.001f, 0.001f)),
            blockCount = math.max(blockCount, math.int2(1, 1)),
            aisleWidth = math.max(aisleWidth, math.float2(0.0f, 0.0f))
        };

        public (int2 block, int2 seat) GetCoordinatesFromIndex(int i)
        {
            var si = i / BlockSeatCount;
            var pi = i - BlockSeatCount * si;
            var sy = si / blockCount.x;
            var sx = si - blockCount.x * sy;
            var py = pi / seatPerBlock.x;
            var px = pi - seatPerBlock.x * py;

            return (math.int2(sx, sy), math.int2(px, py));
        }

        public float2 GetPositionOnPlane(int2 block, int2 seat)
        {
            var lastSeat = seatPerBlock - math.int2(1, 1);
            var lastBlock = blockCount - math.int2(1, 1);

            return seatPitch * (seat - (float2)lastSeat * 0.5f)
                   + (seatPitch * lastSeat + aisleWidth)
                   * (block - (float2)lastBlock * 0.5f);
        }
    }
}
