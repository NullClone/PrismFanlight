using System;
using PrismFanlight.Rendering;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public class SeatLayout : IEquatable<SeatLayout>
    {
        // Fields

        public int2 seatPerBlock;
        public float2 seatPitch;
        public int2 blockCount;
        public float2 aisleWidth;
        public FanlightBlockTransform[] blockTransforms;

        [SerializeField]
        private int _bakedAuthoringHash;

        [SerializeField]
        private FanlightSeatData[] _bakedSeats;

        [SerializeField]
        private FanlightBakedBlockData[] _bakedBlocks;

        [SerializeField]
        private Bounds _bakedBounds;


        // Properties

        public static SeatLayout Default() => new()
        {
            seatPerBlock = math.int2(8, 12),
            seatPitch = math.float2(0.4f, 0.8f),
            blockCount = math.int2(7, 3),
            aisleWidth = math.float2(0.7f, 1.2f),
            blockTransforms = Array.Empty<FanlightBlockTransform>()
        };

        public int BlockSeatCount => seatPerBlock.x * seatPerBlock.y;

        public int TotalBlockCount => blockCount.x * blockCount.y;

        public int TotalSeatCount => seatPerBlock.x * seatPerBlock.y * blockCount.x * blockCount.y;

        public bool HasValidBake => IsBakedDataValid();

        public bool NeedsBake => !HasValidBake;

        public int AuthoringHash => ComputeAuthoringHash();


        // Methods

        public SeatLayout Validated()
        {
            var layout = new SeatLayout
            {
                seatPerBlock = math.max(seatPerBlock, math.int2(1, 1)),
                seatPitch = math.max(seatPitch, math.float2(0.001f, 0.001f)),
                blockCount = math.max(blockCount, math.int2(1, 1)),
                aisleWidth = math.max(aisleWidth, math.float2(0.0f, 0.0f))
            };

            layout.blockTransforms = NormalizeBlockTransforms(blockTransforms, layout.TotalBlockCount);

            if (IsBakedDataValidFor(layout.ComputeAuthoringHash(), layout.TotalSeatCount, layout.TotalBlockCount))
            {
                layout._bakedAuthoringHash = _bakedAuthoringHash;
                layout._bakedSeats = _bakedSeats;
                layout._bakedBlocks = _bakedBlocks;
                layout._bakedBounds = _bakedBounds;
            }

            return layout;
        }

        public bool Equals(SeatLayout other)
        {
            return other != null
                   && seatPerBlock.Equals(other.seatPerBlock)
                   && seatPitch.Equals(other.seatPitch)
                   && blockCount.Equals(other.blockCount)
                   && aisleWidth.Equals(other.aisleWidth)
                   && BlockTransformsEqual(blockTransforms, other.blockTransforms)
                   && _bakedAuthoringHash == other._bakedAuthoringHash;
        }

        public override bool Equals(object obj)
        {
            return obj is SeatLayout other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + seatPerBlock.GetHashCode();
                hash = hash * 31 + seatPitch.GetHashCode();
                hash = hash * 31 + blockCount.GetHashCode();
                hash = hash * 31 + aisleWidth.GetHashCode();
                hash = hash * 31 + HashBlockTransforms(blockTransforms);
                hash = hash * 31 + _bakedAuthoringHash;
                return hash;
            }
        }

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

        public int GetBlockIndex(int2 block) => block.y * blockCount.x + block.x;

        public int2 GetBlockCoordinates(int blockIndex)
        {
            var y = blockIndex / blockCount.x;
            var x = blockIndex - y * blockCount.x;
            return math.int2(x, y);
        }

        public FanlightBlockTransform GetBlockTransform(int2 block)
        {
            var index = GetBlockIndex(block);
            return blockTransforms != null && index >= 0 && index < blockTransforms.Length
                ? blockTransforms[index]
                : FanlightBlockTransform.Identity;
        }

        public Vector3 GetBlockBaseCenterLocal(int2 block)
        {
            var min = GetPositionOnPlane(block, math.int2(0, 0)) - seatPitch * 0.5f;
            var max = GetPositionOnPlane(block, seatPerBlock - math.int2(1, 1)) + seatPitch * 0.5f;
            var center = (min + max) * 0.5f;
            return new Vector3(center.x, 0f, center.y);
        }

        public Vector3 GetBlockCenterLocal(int2 block)
        {
            var placement = GetBlockTransform(block);
            return GetBlockBaseCenterLocal(block) + placement.position;
        }

        public Vector3 GetSeatLocalPosition(int2 block, int2 seat)
        {
            var plane = GetPositionOnPlane(block, seat);
            return TransformBlockPoint(block, new Vector3(plane.x, 0f, plane.y));
        }

        public Vector3 TransformBlockPoint(int2 block, Vector3 localPoint)
        {
            var baseCenter = GetBlockBaseCenterLocal(block);
            var placement = GetBlockTransform(block);
            return baseCenter + placement.position + placement.Rotation * (localPoint - baseCenter);
        }

        public void SetBakedGeometry(FanlightSeatData[] seats, FanlightBakedBlockData[] blocks, Bounds bounds)
        {
            _bakedAuthoringHash = ComputeAuthoringHash();
            _bakedSeats = seats ?? Array.Empty<FanlightSeatData>();
            _bakedBlocks = blocks ?? Array.Empty<FanlightBakedBlockData>();
            _bakedBounds = bounds;
        }

        public void ClearBakedGeometry()
        {
            _bakedAuthoringHash = 0;
            _bakedSeats = null;
            _bakedBlocks = null;
            _bakedBounds = default;
        }

        public bool TryGetBakedSeats(out FanlightSeatData[] seats)
        {
            seats = HasValidBake ? _bakedSeats : null;
            return seats != null;
        }

        public bool TryGetBakedBlocks(out FanlightBakedBlockData[] blocks)
        {
            blocks = HasValidBake ? _bakedBlocks : null;
            return blocks != null;
        }

        public bool TryGetBakedBounds(out Bounds bounds)
        {
            bounds = HasValidBake ? _bakedBounds : default;
            return HasValidBake;
        }

        private bool IsBakedDataValid()
        {
            return IsBakedDataValidFor(ComputeAuthoringHash(), TotalSeatCount, TotalBlockCount);
        }

        private bool IsBakedDataValidFor(int authoringHash, int seatCount, int totalBlockCount)
        {
            return _bakedAuthoringHash == authoringHash
                   && _bakedSeats != null
                   && _bakedSeats.Length == seatCount
                   && _bakedBlocks != null
                   && _bakedBlocks.Length == totalBlockCount;
        }

        private int ComputeAuthoringHash()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + seatPerBlock.GetHashCode();
                hash = hash * 31 + seatPitch.GetHashCode();
                hash = hash * 31 + blockCount.GetHashCode();
                hash = hash * 31 + aisleWidth.GetHashCode();
                hash = hash * 31 + HashBlockTransforms(NormalizeBlockTransforms(blockTransforms, TotalBlockCount));
                return hash;
            }
        }

        private static FanlightBlockTransform[] NormalizeBlockTransforms(FanlightBlockTransform[] source, int count)
        {
            if (count <= 0)
            {
                return Array.Empty<FanlightBlockTransform>();
            }

            var result = new FanlightBlockTransform[count];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = source != null && i < source.Length
                    ? source[i]
                    : FanlightBlockTransform.Identity;
            }

            return result;
        }

        private static bool BlockTransformsEqual(FanlightBlockTransform[] a, FanlightBlockTransform[] b)
        {
            var count = Math.Max(a?.Length ?? 0, b?.Length ?? 0);

            for (var i = 0; i < count; i++)
            {
                var left = a != null && i < a.Length ? a[i] : FanlightBlockTransform.Identity;
                var right = b != null && i < b.Length ? b[i] : FanlightBlockTransform.Identity;

                if (!left.Equals(right))
                {
                    return false;
                }
            }

            return true;
        }

        private static int HashBlockTransforms(FanlightBlockTransform[] transforms)
        {
            unchecked
            {
                var hash = 17;
                var count = transforms?.Length ?? 0;

                for (var i = 0; i < count; i++)
                {
                    hash = hash * 31 + transforms[i].GetHashCode();
                }

                return hash;
            }
        }
    }
}
