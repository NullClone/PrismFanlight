using System;
using System.Collections.Generic;
using System.IO;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightCompiledLayout
    {
        public FanlightCompiledLayout(FanlightLayoutAsset source)
        {
            Source = source;
            Seats = new FanlightBakedSeatRecord[source.TotalSeatCount];
            Blocks = new FanlightBakedBlockRecord[source.TotalBlockCount];

            for (var i = 0; i < Blocks.Length; i++) CompileBlock(i);

            RecalculateSummary();
        }

        public FanlightLayoutAsset Source { get; }

        public FanlightBakedSeatRecord[] Seats { get; }

        public FanlightBakedBlockRecord[] Blocks { get; }

        public Bounds LocalBounds { get; private set; }

        public ulong ContentHash { get; private set; }

        public void SetSummary(Bounds localBounds, ulong contentHash)
        {
            LocalBounds = localBounds;
            ContentHash = contentHash == 0UL ? 1UL : contentHash;
        }

        public void CompileBlock(int blockIndex)
        {
            var block = Source.GetBlockCoordinates(blockIndex);
            var start = blockIndex * Source.BlockSeatCount;
            var hash = FanlightStableHash.Begin();

            for (var y = 0; y < Source.SeatPerBlock.y; y++)
            {
                for (var x = 0; x < Source.SeatPerBlock.x; x++)
                {
                    var localSeat = math.int2(x, y);
                    var plane = Source.GetPositionOnPlane(block, localSeat);
                    var local = Source.TransformBlockPoint(blockIndex, new Vector3(plane.x, 0f, plane.y));
                    var seatIndex = start + y * Source.SeatPerBlock.x + x;
                    var stableSeatId = Source.GetStableSeatId(seatIndex);
                    Seats[seatIndex] = new FanlightBakedSeatRecord
                    {
                        stableSeatId = stableSeatId,
                        localPosition = local,
                        planePosition = new Vector2(plane.x, plane.y),
                        blockCoordinates = new Vector2(block.x, block.y),
                        blockIndex = blockIndex,
                        placementFlags = 1u
                    };
                    hash = FanlightStableHash.Add(hash, stableSeatId);
                    hash = FanlightStableHash.Add(hash, local);
                }
            }

            var bounds = BuildBlockBounds(Source, blockIndex);
            hash = FanlightStableHash.Add(hash, bounds.center);
            hash = FanlightStableHash.Add(hash, bounds.size);
            Blocks[blockIndex] = new FanlightBakedBlockRecord
            {
                blockId = Source.GetBlock(blockIndex).BlockId,
                localBounds = bounds,
                contiguousSeatStart = start,
                contiguousSeatCount = Source.BlockSeatCount,
                contentHash = FanlightStableHash.Finish(hash)
            };
        }

        public void RecalculateSummary()
        {
            var hasBounds = false;
            var bounds = default(Bounds);
            var hashTree = new FanlightHashTree(Blocks.Length);

            for (var i = 0; i < Blocks.Length; i++)
            {
                var block = Blocks[i];
                hashTree.Update(i, block.contentHash);
                if (!hasBounds)
                {
                    bounds = block.localBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(block.localBounds.min);
                    bounds.Encapsulate(block.localBounds.max);
                }
            }

            var hash = FanlightStableHash.Begin();
            hash = FanlightStableHash.Add(hash, Source.LayoutId.Value);
            hash = FanlightStableHash.Add(hash, hashTree.Root);
            LocalBounds = hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
            ContentHash = FanlightStableHash.Finish(hash);
        }

        private static Bounds BuildBlockBounds(FanlightLayoutAsset layout, int blockIndex)
        {
            var block = layout.GetBlockCoordinates(blockIndex);
            var min2 = layout.GetPositionOnPlane(block, math.int2(0, 0)) - layout.SeatPitch * 0.5f;
            var max2 = layout.GetPositionOnPlane(block, layout.SeatPerBlock - math.int2(1, 1)) + layout.SeatPitch * 0.5f;
            var min = new Vector3(min2.x, -4f, min2.y);
            var max = new Vector3(max2.x, 4f, max2.y);
            var bounds = new Bounds(layout.TransformBlockPoint(blockIndex, min), Vector3.zero);
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(max.x, min.y, min.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(min.x, max.y, min.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(max.x, max.y, min.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(min.x, min.y, max.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(max.x, min.y, max.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(min.x, max.y, max.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, max));
            return bounds;
        }
    }

    internal static class FanlightStableHash
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Begin() => Offset;

        public static ulong Finish(ulong hash) => hash == 0UL ? 1UL : hash;

        public static ulong Add(ulong hash, int value) => Add(hash, unchecked((uint)value));

        public static ulong Add(ulong hash, uint value)
        {
            for (var i = 0; i < 4; i++) hash = AddByte(hash, (byte)(value >> (i * 8)));
            return hash;
        }

        public static ulong Add(ulong hash, ulong value)
        {
            for (var i = 0; i < 8; i++) hash = AddByte(hash, (byte)(value >> (i * 8)));
            return hash;
        }

        public static ulong Add(ulong hash, float value) => Add(hash, BitConverter.SingleToInt32Bits(value));

        public static ulong Add(ulong hash, Vector3 value)
        {
            hash = Add(hash, value.x);
            hash = Add(hash, value.y);
            return Add(hash, value.z);
        }

        public static ulong Add(ulong hash, string value)
        {
            value ??= string.Empty;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                hash = AddByte(hash, (byte)c);
                hash = AddByte(hash, (byte)(c >> 8));
            }

            return hash;
        }

        private static ulong AddByte(ulong hash, byte value) => (hash ^ value) * Prime;
    }

    internal static class FanlightLayoutBakeFile
    {
        private const ulong Magic = 0x31454B41424C4650UL; // PFLBAKE1

        public static void Write(string projectRelativePath, FanlightCompiledLayout compiled)
        {
            var absolutePath = Path.GetFullPath(projectRelativePath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = absolutePath + ".tmp";

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(FanlightLayoutBakeArtifact.CurrentFormatVersion);
                writer.Write(compiled.Source.LayoutId.Value);
                writer.Write(compiled.ContentHash);
                WriteBounds(writer, compiled.LocalBounds);
                writer.Write(compiled.Seats.Length);
                writer.Write(compiled.Blocks.Length);

                for (var blockIndex = 0; blockIndex < compiled.Blocks.Length; blockIndex++)
                {
                    var block = compiled.Blocks[blockIndex];
                    writer.Write(block.blockId ?? string.Empty);
                    WriteBounds(writer, block.localBounds);
                    writer.Write(block.contiguousSeatStart);
                    writer.Write(block.contiguousSeatCount);
                    writer.Write(block.contentHash);

                    var end = block.contiguousSeatStart + block.contiguousSeatCount;
                    for (var seatIndex = block.contiguousSeatStart; seatIndex < end; seatIndex++)
                    {
                        WriteSeat(writer, compiled.Seats[seatIndex]);
                    }
                }
            }

            if (File.Exists(absolutePath)) File.Replace(tempPath, absolutePath, null);
            else File.Move(tempPath, absolutePath);
        }

        public static FanlightBakeFileData Read(string absolutePath)
        {
            using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt64() != Magic) throw new InvalidDataException("Invalid Prism Fanlight layout bake magic.");
            var version = reader.ReadInt32();
            if (version != FanlightLayoutBakeArtifact.CurrentFormatVersion) throw new InvalidDataException($"Unsupported layout bake version: {version}.");

            var data = new FanlightBakeFileData
            {
                LayoutId = reader.ReadString(),
                ContentHash = reader.ReadUInt64(),
                LocalBounds = ReadBounds(reader)
            };
            var seatCount = ReadCount(reader, "seat");
            var blockCount = ReadCount(reader, "block");
            data.Seats = new FanlightBakedSeatRecord[seatCount];
            data.Blocks = new FanlightBakedBlockRecord[blockCount];
            var assignedSeats = new bool[seatCount];
            var stableSeatIds = new HashSet<ulong>();
            var hashTree = new FanlightHashTree(blockCount);

            for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                var block = new FanlightBakedBlockRecord
                {
                    blockId = reader.ReadString(),
                    localBounds = ReadBounds(reader),
                    contiguousSeatStart = reader.ReadInt32(),
                    contiguousSeatCount = reader.ReadInt32(),
                    contentHash = reader.ReadUInt64()
                };
                if (block.contiguousSeatStart < 0 || block.contiguousSeatCount <= 0
                                                  || block.contiguousSeatCount > seatCount
                                                  || block.contiguousSeatStart > seatCount - block.contiguousSeatCount)
                {
                    throw new InvalidDataException($"Invalid seat range in block {blockIndex}.");
                }

                data.Blocks[blockIndex] = block;
                var end = block.contiguousSeatStart + block.contiguousSeatCount;
                var blockHash = FanlightStableHash.Begin();
                for (var seatIndex = block.contiguousSeatStart; seatIndex < end; seatIndex++)
                {
                    if (assignedSeats[seatIndex]) throw new InvalidDataException($"Overlapping seat range at seat {seatIndex}.");
                    assignedSeats[seatIndex] = true;
                    var seat = ReadSeat(reader);
                    if (seat.stableSeatId == 0UL || !stableSeatIds.Add(seat.stableSeatId))
                    {
                        throw new InvalidDataException($"Invalid or duplicate stable seat ID at seat {seatIndex}.");
                    }

                    if (seat.blockIndex != blockIndex) throw new InvalidDataException($"Seat {seatIndex} references the wrong block.");
                    data.Seats[seatIndex] = seat;
                    blockHash = FanlightStableHash.Add(blockHash, seat.stableSeatId);
                    blockHash = FanlightStableHash.Add(blockHash, seat.localPosition);
                }

                blockHash = FanlightStableHash.Add(blockHash, block.localBounds.center);
                blockHash = FanlightStableHash.Add(blockHash, block.localBounds.size);
                blockHash = FanlightStableHash.Finish(blockHash);
                if (blockHash != block.contentHash) throw new InvalidDataException($"Block {blockIndex} content hash mismatch.");
                hashTree.Update(blockIndex, blockHash);
            }

            for (var i = 0; i < assignedSeats.Length; i++)
            {
                if (!assignedSeats[i]) throw new InvalidDataException($"Seat {i} is not owned by a block.");
            }

            var layoutHash = FanlightStableHash.Begin();
            layoutHash = FanlightStableHash.Add(layoutHash, data.LayoutId);
            layoutHash = FanlightStableHash.Add(layoutHash, hashTree.Root);
            if (FanlightStableHash.Finish(layoutHash) != data.ContentHash) throw new InvalidDataException("Layout content hash mismatch.");
            if (stream.Position != stream.Length) throw new InvalidDataException("Unexpected trailing data in layout bake.");
            return data;
        }

        private static int ReadCount(BinaryReader reader, string label)
        {
            var count = reader.ReadInt32();
            if (count < 0 || count > 100_000_000) throw new InvalidDataException($"Invalid {label} count: {count}.");
            return count;
        }

        private static void WriteSeat(BinaryWriter writer, FanlightBakedSeatRecord seat)
        {
            writer.Write(seat.stableSeatId);
            WriteVector3(writer, seat.localPosition);
            writer.Write(seat.planePosition.x);
            writer.Write(seat.planePosition.y);
            writer.Write(seat.blockCoordinates.x);
            writer.Write(seat.blockCoordinates.y);
            writer.Write(seat.blockIndex);
            writer.Write(seat.placementFlags);
        }

        private static FanlightBakedSeatRecord ReadSeat(BinaryReader reader)
        {
            return new FanlightBakedSeatRecord
            {
                stableSeatId = reader.ReadUInt64(),
                localPosition = ReadVector3(reader),
                planePosition = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                blockCoordinates = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                blockIndex = reader.ReadInt32(),
                placementFlags = reader.ReadUInt32()
            };
        }

        private static void WriteBounds(BinaryWriter writer, Bounds bounds)
        {
            WriteVector3(writer, bounds.center);
            WriteVector3(writer, bounds.size);
        }

        private static Bounds ReadBounds(BinaryReader reader) => new(ReadVector3(reader), ReadVector3(reader));

        private static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        private static Vector3 ReadVector3(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    internal sealed class FanlightBakeFileData
    {
        public string LayoutId;
        public ulong ContentHash;
        public Bounds LocalBounds;
        public FanlightBakedSeatRecord[] Seats;
        public FanlightBakedBlockRecord[] Blocks;
    }

    [ScriptedImporter(FanlightLayoutBakeArtifact.CurrentFormatVersion, "pflayoutbake")]
    internal sealed class FanlightLayoutBakeImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            try
            {
                var data = FanlightLayoutBakeFile.Read(context.assetPath);
                var artifact = ScriptableObject.CreateInstance<FanlightLayoutBakeArtifact>();
                artifact.name = Path.GetFileNameWithoutExtension(context.assetPath);
                artifact.InitializeImported(data.LayoutId, data.ContentHash, data.LocalBounds, data.Seats, data.Blocks);
                context.AddObjectToAsset("LayoutBake", artifact);
                context.SetMainObject(artifact);
            }
            catch (Exception exception)
            {
                context.LogImportError($"Failed to import Prism Fanlight layout bake: {exception.Message}");
            }
        }
    }
}
