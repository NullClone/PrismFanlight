using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutGenerator
    {
        internal enum Shape
        {
            Rectangle,
            Trapezoid,
            Fan,
            Raked
        }

        internal enum SeatAnchor
        {
            Left,
            Center,
            Right
        }


        // Methods

        internal static FanlightLayoutRow[] Generate(
            FanlightLayoutAsset layout,
            FanlightLayoutBlock existingBlock,
            Shape shape,
            int rowCount,
            int seatCount,
            float width,
            float backWidth,
            float depth,
            float rise,
            float curve,
            SeatAnchor seatAnchor)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            rowCount = Mathf.Clamp(rowCount, 1, 512);
            seatCount = Mathf.Clamp(seatCount, 1, 4096);
            width = Mathf.Max(0.001f, width);
            backWidth = Mathf.Max(0.001f, backWidth);
            depth = Mathf.Max(0f, depth);

            var rows = new FanlightLayoutRow[rowCount];
            var reserved = new HashSet<ulong>();
            layout.CollectStableSeatIds(reserved);

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var t = rowCount == 1 ? 0.5f : (float)rowIndex / (rowCount - 1);
                var z = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, t);
                var y = shape == Shape.Raked ? rise * t : Mathf.Lerp(0f, rise, t);
                var rowWidth = shape == Shape.Trapezoid || shape == Shape.Fan
                    ? Mathf.Lerp(width, backWidth, t)
                    : width;
                var left = new Vector3(-rowWidth * 0.5f, y, z);
                var right = new Vector3(rowWidth * 0.5f, y, z);
                var control = (left + right) * 0.5f;
                if (shape == Shape.Fan) control.z += curve;

                var existingRow = existingBlock != null && rowIndex < existingBlock.RowCount
                    ? existingBlock.GetRow(rowIndex)
                    : null;
                var ids = ResizeStableSeatIds(existingRow, seatCount, seatAnchor, reserved);
                rows[rowIndex] = new FanlightLayoutRow(left, control, right, ids);
            }

            return rows;
        }

        internal static FanlightLayoutBlock[] GenerateQuickGrid(
            int2 blockCount,
            int2 seatsPerBlock,
            float2 seatSpacing,
            float2 aisleWidth)
        {
            blockCount = math.max(blockCount, math.int2(1, 1));
            seatsPerBlock = math.max(seatsPerBlock, math.int2(1, 1));
            seatSpacing = math.max(seatSpacing, math.float2(0.001f, 0.001f));
            aisleWidth = math.max(aisleWidth, float2.zero);

            var totalBlocks = checked(blockCount.x * blockCount.y);
            var blocks = new FanlightLayoutBlock[totalBlocks];
            var reservedBlockIds = new HashSet<string>(StringComparer.Ordinal);
            var reservedSeatIds = new HashSet<ulong>();
            var blockWidth = (seatsPerBlock.x - 1) * seatSpacing.x;
            var blockDepth = (seatsPerBlock.y - 1) * seatSpacing.y;
            var blockStep = new float2(blockWidth + aisleWidth.x, blockDepth + aisleWidth.y);
            var gridCenter = new float2(
                (blockCount.x - 1) * blockStep.x * 0.5f,
                (blockCount.y - 1) * blockStep.y * 0.5f);

            for (var blockZ = 0; blockZ < blockCount.y; blockZ++)
            {
                for (var blockX = 0; blockX < blockCount.x; blockX++)
                {
                    var rows = new FanlightLayoutRow[seatsPerBlock.y];
                    for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                    {
                        var z = (rowIndex - (rows.Length - 1) * 0.5f) * seatSpacing.y;
                        var left = new Vector3(-blockWidth * 0.5f, 0f, z);
                        var right = new Vector3(blockWidth * 0.5f, 0f, z);
                        rows[rowIndex] = new FanlightLayoutRow(
                            left,
                            (left + right) * 0.5f,
                            right,
                            FanlightLayoutAsset.CreateStableSeatIds(seatsPerBlock.x, reservedSeatIds));
                    }

                    string blockId;
                    do
                    {
                        blockId = Guid.NewGuid().ToString("N");
                    } while (!reservedBlockIds.Add(blockId));

                    var position2D = new float2(blockX * blockStep.x, blockZ * blockStep.y) - gridCenter;
                    var placement = new FanlightBlockPlacement
                    {
                        position = new Vector3(position2D.x, 0f, position2D.y),
                        eulerRotation = Vector3.zero
                    };
                    blocks[blockZ * blockCount.x + blockX] = new FanlightLayoutBlock(blockId, placement, rows);
                }
            }

            return blocks;
        }

        internal static FanlightLayoutRow[] DuplicateRowsWithNewIds(
            FanlightLayoutAsset layout,
            FanlightLayoutBlock source)
        {
            var reserved = new HashSet<ulong>();
            layout.CollectStableSeatIds(reserved);
            var rows = new FanlightLayoutRow[source.RowCount];

            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = source.GetRow(rowIndex);
                rows[rowIndex] = new FanlightLayoutRow(
                    row.LeftPoint,
                    row.ControlPoint,
                    row.RightPoint,
                    FanlightLayoutAsset.CreateStableSeatIds(row.SeatCount, reserved));
            }

            return rows;
        }

        internal static FanlightLayoutRow[] ResizeBlock(
            FanlightLayoutAsset layout,
            FanlightLayoutBlock block,
            int rowCount,
            int seatCount)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (block == null) throw new ArgumentNullException(nameof(block));

            rowCount = Mathf.Clamp(rowCount, 1, 512);
            seatCount = Mathf.Clamp(seatCount, 1, 4096);
            var rows = new FanlightLayoutRow[rowCount];
            var reserved = new HashSet<ulong>();
            layout.CollectStableSeatIds(reserved);
            var first = block.GetRow(0);
            var last = block.GetRow(block.RowCount - 1);
            var generatedDepth = (rowCount - 1) * layout.ReferenceSeatSpacing.y;

            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var t = rows.Length == 1 ? 0.5f : (float)rowIndex / (rows.Length - 1);
                Vector3 left;
                Vector3 control;
                Vector3 right;
                if (rows.Length == block.RowCount)
                {
                    var source = block.GetRow(rowIndex);
                    left = source.LeftPoint;
                    control = source.ControlPoint;
                    right = source.RightPoint;
                }
                else if (block.RowCount == 1)
                {
                    var offset = Vector3.forward * ((t - 0.5f) * generatedDepth);
                    left = first.LeftPoint + offset;
                    control = first.ControlPoint + offset;
                    right = first.RightPoint + offset;
                }
                else
                {
                    left = Vector3.Lerp(first.LeftPoint, last.LeftPoint, t);
                    control = Vector3.Lerp(first.ControlPoint, last.ControlPoint, t);
                    right = Vector3.Lerp(first.RightPoint, last.RightPoint, t);
                }

                var existing = GetResizeSourceRow(block, rowIndex, rows.Length);
                rows[rowIndex] = new FanlightLayoutRow(
                    left,
                    control,
                    right,
                    ResizeStableSeatIds(existing, seatCount, SeatAnchor.Center, reserved));
            }

            return rows;
        }

        internal static ulong[] ResizeStableSeatIds(
            FanlightLayoutRow existingRow,
            int newCount,
            SeatAnchor anchor,
            HashSet<ulong> reserved)
        {
            newCount = Mathf.Clamp(newCount, 1, 4096);
            reserved ??= new HashSet<ulong>();

            if (existingRow == null)
            {
                return FanlightLayoutAsset.CreateStableSeatIds(newCount, reserved);
            }

            var existing = existingRow.CopyStableSeatIds();
            if (existing.Length == newCount) return existing;

            var result = new ulong[newCount];
            var preserveCount = Mathf.Min(existing.Length, newCount);
            var sourceStart = GetStart(existing.Length, preserveCount, anchor);
            var destinationStart = GetStart(newCount, preserveCount, anchor);
            Array.Copy(existing, sourceStart, result, destinationStart, preserveCount);

            var additions = FanlightLayoutAsset.CreateStableSeatIds(newCount - preserveCount, reserved);
            var additionIndex = 0;
            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] == 0UL) result[i] = additions[additionIndex++];
            }

            return result;
        }


        private static int GetStart(int totalCount, int preserveCount, SeatAnchor anchor)
        {
            return anchor switch
            {
                SeatAnchor.Left => 0,
                SeatAnchor.Right => totalCount - preserveCount,
                _ => (totalCount - preserveCount) / 2
            };
        }

        private static FanlightLayoutRow GetResizeSourceRow(
            FanlightLayoutBlock block,
            int destinationIndex,
            int destinationCount)
        {
            if (block.RowCount == destinationCount) return block.GetRow(destinationIndex);
            if (block.RowCount == 1)
            {
                return destinationIndex == (destinationCount - 1) / 2 ? block.GetRow(0) : null;
            }

            if (destinationCount == 1) return block.GetRow((block.RowCount - 1) / 2);
            if (destinationCount < block.RowCount)
            {
                var sourceIndex = Mathf.RoundToInt(
                    (float)destinationIndex * (block.RowCount - 1) / (destinationCount - 1));
                return block.GetRow(sourceIndex);
            }

            for (var sourceIndex = 0; sourceIndex < block.RowCount; sourceIndex++)
            {
                var mappedDestination = Mathf.RoundToInt(
                    (float)sourceIndex * (destinationCount - 1) / (block.RowCount - 1));
                if (mappedDestination == destinationIndex) return block.GetRow(sourceIndex);
            }

            return null;
        }
    }
}
