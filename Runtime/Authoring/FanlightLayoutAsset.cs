using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PrismFanlight.Core;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [PreferBinarySerialization]
    public sealed class FanlightLayoutAsset : ScriptableObject
    {
        // Fields

        [SerializeField]
        private string _layoutId;

        [SerializeField]
        private ulong _contentHash;

        [SerializeField]
        private int2 _seatPerBlock = new(8, 12);

        [SerializeField]
        private float2 _seatPitch = new(0.4f, 0.8f);

        [SerializeField]
        private int2 _blockCount = new(7, 3);

        [SerializeField]
        private float2 _aisleWidth = new(0.7f, 1.2f);

        [SerializeField]
        private FanlightLayoutBlock[] _blocks = Array.Empty<FanlightLayoutBlock>();

        [SerializeField]
        private ulong[] _stableSeatIds = Array.Empty<ulong>();

        [SerializeField]
        private FanlightLayoutBakeArtifact _activeBake;


        // Properties

        public FanlightLayoutId LayoutId => new(_layoutId);

        public ulong ContentHash => _contentHash;

        public int2 SeatPerBlock => _seatPerBlock;

        public float2 SeatPitch => _seatPitch;

        public int2 BlockCount => _blockCount;

        public float2 AisleWidth => _aisleWidth;

        public int BlockSeatCount => TryGetTopologyCounts(out var blockSeatCount, out _, out _) ? blockSeatCount : 0;

        public int TotalBlockCount => TryGetTopologyCounts(out _, out var totalBlockCount, out _) ? totalBlockCount : 0;

        public int TotalSeatCount => TryGetTopologyCounts(out _, out _, out var totalSeatCount) ? totalSeatCount : 0;

        public FanlightLayoutBakeArtifact ActiveBake => _activeBake;

        public bool IsInitialized => LayoutId.IsValid
                                     && TryGetTopologyCounts(out _, out var totalBlockCount, out var totalSeatCount)
                                     && _blocks != null
                                     && _blocks.Length == totalBlockCount
                                     && _stableSeatIds != null
                                     && _stableSeatIds.Length == totalSeatCount;

        public bool HasValidBake => IsInitialized && _activeBake != null && _activeBake.Matches(this);


        // Methods

        public FanlightLayoutBlock GetBlock(int blockIndex) => _blocks[blockIndex];

        public ulong GetStableSeatId(int seatIndex) => _stableSeatIds[seatIndex];

        public int2 GetBlockCoordinates(int blockIndex)
        {
            var y = blockIndex / _blockCount.x;
            return math.int2(blockIndex - y * _blockCount.x, y);
        }

        public float2 GetPositionOnPlane(int2 block, int2 seat)
        {
            var lastSeat = _seatPerBlock - math.int2(1, 1);
            var lastBlock = _blockCount - math.int2(1, 1);
            return _seatPitch * (seat - (float2)lastSeat * 0.5f)
                   + (_seatPitch * lastSeat + _aisleWidth) * (block - (float2)lastBlock * 0.5f);
        }

        public Vector3 GetBlockBaseCenterLocal(int2 block)
        {
            var min = GetPositionOnPlane(block, math.int2(0, 0)) - _seatPitch * 0.5f;
            var max = GetPositionOnPlane(block, _seatPerBlock - math.int2(1, 1)) + _seatPitch * 0.5f;
            var center = (min + max) * 0.5f;
            return new Vector3(center.x, 0f, center.y);
        }

        public Vector3 TransformBlockPoint(int blockIndex, Vector3 point)
        {
            var block = GetBlockCoordinates(blockIndex);
            var baseCenter = GetBlockBaseCenterLocal(block);
            var placement = _blocks[blockIndex].Placement;
            return baseCenter + placement.position + placement.Rotation * (point - baseCenter);
        }


        internal void Rebuild(int2 seatPerBlock, float2 seatPitch, int2 blockCount, float2 aisleWidth)
        {
            if (!math.all(math.isfinite(seatPitch)) || !math.all(math.isfinite(aisleWidth)))
            {
                throw new ArgumentOutOfRangeException(nameof(seatPitch), "Layout spacing values must be finite.");
            }

            seatPerBlock = math.max(seatPerBlock, math.int2(1, 1));
            seatPitch = math.max(seatPitch, math.float2(0.001f, 0.001f));
            blockCount = math.max(blockCount, math.int2(1, 1));
            aisleWidth = math.max(aisleWidth, float2.zero);

            int totalBlocks;
            int totalSeats;

            try
            {
                var totalBlocks64 = checked((long)blockCount.x * blockCount.y);
                var blockSeats64 = checked((long)seatPerBlock.x * seatPerBlock.y);
                var totalSeats64 = checked(totalBlocks64 * blockSeats64);

                if (totalBlocks64 > int.MaxValue || blockSeats64 > int.MaxValue || totalSeats64 > int.MaxValue)
                {
                    throw new OverflowException();
                }

                totalBlocks = (int)totalBlocks64;
                totalSeats = (int)totalSeats64;
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(blockCount), "Layout topology exceeds the supported 32-bit artifact range.");
            }

            var layoutId = Guid.NewGuid().ToString("N");
            var seatIds = CreateSeatIds(totalSeats);
            var blockIds = new string[totalBlocks];
            var usedBlockIds = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < blockIds.Length; i++)
            {
                string blockId;

                do
                {
                    blockId = Guid.NewGuid().ToString("N");
                } while (!usedBlockIds.Add(blockId));

                blockIds[i] = blockId;
            }

            var normalizedLayoutId = new FanlightLayoutId(layoutId);

            _layoutId = normalizedLayoutId.Value;
            _contentHash = 0UL;
            _seatPerBlock = seatPerBlock;
            _seatPitch = seatPitch;
            _blockCount = blockCount;
            _aisleWidth = aisleWidth;
            _blocks = new FanlightLayoutBlock[totalBlocks];
            for (var i = 0; i < totalBlocks; i++)
            {
                _blocks[i] = new FanlightLayoutBlock(blockIds[i]);
            }

            _stableSeatIds = seatIds;
            _activeBake = null;
        }

        internal bool SetBlockPlacement(int blockIndex, FanlightBlockPlacement placement)
        {
            if (!IsInitialized || blockIndex < 0 || blockIndex >= _blocks.Length) return false;
            if (_blocks[blockIndex].Placement.Equals(placement)) return false;

            _blocks[blockIndex].SetPlacement(placement);

            return true;
        }

        internal bool SetContentHash(ulong contentHash)
        {
            contentHash = contentHash == 0UL ? 1UL : contentHash;

            if (_contentHash == contentHash) return false;

            _contentHash = contentHash;

            return true;
        }

        internal void SetActiveBake(FanlightLayoutBakeArtifact artifact)
        {
            _activeBake = artifact;
        }

        private bool TryGetTopologyCounts(out int blockSeatCount, out int totalBlockCount, out int totalSeatCount)
        {
            blockSeatCount = 0;
            totalBlockCount = 0;
            totalSeatCount = 0;

            if (_seatPerBlock.x <= 0 || _seatPerBlock.y <= 0 || _blockCount.x <= 0 || _blockCount.y <= 0) return false;

            var blockSeats64 = (long)_seatPerBlock.x * _seatPerBlock.y;
            var totalBlocks64 = (long)_blockCount.x * _blockCount.y;

            if (blockSeats64 > int.MaxValue || totalBlocks64 > int.MaxValue) return false;

            var totalSeats64 = blockSeats64 * totalBlocks64;

            if (totalSeats64 > int.MaxValue) return false;

            blockSeatCount = (int)blockSeats64;
            totalBlockCount = (int)totalBlocks64;
            totalSeatCount = (int)totalSeats64;

            return true;
        }


        private static ulong[] CreateSeatIds(int count)
        {
            var values = new ulong[count];
            var used = new HashSet<ulong>();
            var bytes = new byte[8];

            using var random = RandomNumberGenerator.Create();

            for (var i = 0; i < values.Length; i++)
            {
                ulong value;
                do
                {
                    random.GetBytes(bytes);
                    value = BitConverter.ToUInt64(bytes, 0);
                } while (value == 0UL || !used.Add(value));

                values[i] = value;
            }

            return values;
        }
    }
}
