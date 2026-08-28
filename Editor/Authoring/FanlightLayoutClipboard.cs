using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutClipboard
    {
        private sealed class BlockSnapshot
        {
            // Properties

            internal FanlightBlockPlacement Placement { get; }

            internal FanlightLayoutRow[] Rows { get; }


            // Methods

            internal BlockSnapshot(FanlightBlockPlacement placement, FanlightLayoutRow[] rows)
            {
                Placement = placement;
                Rows = rows;
            }
        }


        // Fields

        private static BlockSnapshot[] _blocks = Array.Empty<BlockSnapshot>();
        private static Vector2 _anchor;


        // Properties

        internal static bool CanPaste => _blocks.Length > 0;


        // Methods

        internal static bool Copy(FanlightLayoutAsset layout, IReadOnlyList<int> blockIndices)
        {
            if (layout == null || blockIndices == null || blockIndices.Count == 0) return false;

            var blocks = new BlockSnapshot[blockIndices.Count];
            var session = FanlightLayoutEditSession.Get(layout);

            if (session == null || blockIndices[0] < 0 || blockIndices[0] >= layout.BlockCount) return false;

            var bounds = session.GetBlockBounds(blockIndices[0]);

            for (var i = 0; i < blocks.Length; i++)
            {
                var blockIndex = blockIndices[i];
                if (blockIndex < 0 || blockIndex >= layout.BlockCount) return false;

                var block = layout.GetBlock(blockIndex);
                var placement = block.Placement;
                if (i > 0) bounds.Encapsulate(session.GetBlockBounds(blockIndex));
                blocks[i] = new BlockSnapshot(placement, CloneRows(block));
            }

            _blocks = blocks;
            _anchor = new Vector2(bounds.center.x, bounds.center.z);

            return true;
        }

        internal static bool Paste(FanlightLayoutAsset layout, Vector3 layoutPosition)
        {
            if (layout == null || !layout.IsInitialized || !CanPaste) return false;

            var reserved = new HashSet<ulong>();
            layout.CollectStableSeatIds(reserved);
            var created = new List<int>(_blocks.Length);
            var delta = new Vector3(layoutPosition.x - _anchor.x, 0f, layoutPosition.z - _anchor.y);
            if (!FanlightLayoutEditSession.ApplyTopologyChange(
                    layout,
                    "Paste Fanlight Blocks",
                    () =>
                    {
                        for (var blockIndex = 0; blockIndex < _blocks.Length; blockIndex++)
                        {
                            var source = _blocks[blockIndex];
                            var rows = new FanlightLayoutRow[source.Rows.Length];
                            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                            {
                                var row = source.Rows[rowIndex];
                                rows[rowIndex] = new FanlightLayoutRow(
                                    row.LeftPoint,
                                    row.ControlPoint,
                                    row.RightPoint,
                                    FanlightLayoutAsset.CreateStableSeatIds(row.SeatCount, reserved));
                            }

                            var placement = source.Placement;
                            placement.position += delta;
                            if (!layout.AddBlock(layout.CreateBlock(rows, placement))) return false;
                            created.Add(layout.BlockCount - 1);
                        }

                        return true;
                    }))
            {
                return false;
            }

            FanlightLayoutSelection.SetIndices(layout, created);

            return true;
        }

        private static FanlightLayoutRow[] CloneRows(FanlightLayoutBlock block)
        {
            var rows = new FanlightLayoutRow[block.RowCount];
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = block.GetRow(rowIndex);
                rows[rowIndex] = new FanlightLayoutRow(
                    row.LeftPoint,
                    row.ControlPoint,
                    row.RightPoint,
                    row.CopyStableSeatIds());
            }

            return rows;
        }
    }
}
