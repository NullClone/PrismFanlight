using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutCommands
    {
        // Methods

        internal static void AddBlock(
            FanlightLayoutAsset layout,
            FanlightLayoutGenerator.Shape shape,
            Vector3 layoutPosition)
        {
            if (layout == null) return;

            var spacing = layout.ReferenceSeatSpacing;
            var rowCount = 12;
            var seatCount = 8;
            var width = (seatCount - 1) * spacing.x;
            var depth = (rowCount - 1) * spacing.y;
            var rows = FanlightLayoutGenerator.Generate(
                layout,
                null,
                shape,
                rowCount,
                seatCount,
                width,
                width * 1.3f,
                depth,
                shape == FanlightLayoutGenerator.Shape.Raked ? depth * 0.2f : 0f,
                shape == FanlightLayoutGenerator.Shape.Fan ? spacing.y : 0f,
                FanlightLayoutGenerator.SeatAnchor.Center);

            var block = layout.CreateBlock(rows, new FanlightBlockPlacement
            {
                position = new Vector3(layoutPosition.x, 0f, layoutPosition.z),
                eulerRotation = Vector3.zero
            });

            if (FanlightLayoutEditSession.ApplyTopologyChange(
                    layout,
                    "Add Fanlight Block",
                    () => layout.AddBlock(block)))
            {
                FanlightLayoutSelection.SetOnly(layout, layout.BlockCount - 1);
            }
        }

        internal static void Duplicate(FanlightLayoutAsset layout)
        {
            var selected = GetSelected(layout);
            if (selected.Count == 0) return;

            var created = new List<int>();
            if (!FanlightLayoutEditSession.ApplyTopologyChange(
                    layout,
                    "Duplicate Fanlight Blocks",
                    () =>
                    {
                        for (var i = 0; i < selected.Count; i++)
                        {
                            var source = layout.GetBlock(selected[i]);
                            var placement = source.Placement;
                            placement.position.x += layout.ReferenceSeatSpacing.x * 2f;
                            var duplicate = layout.CreateBlock(
                                FanlightLayoutGenerator.DuplicateRowsWithNewIds(layout, source),
                                placement);
                            if (!layout.AddBlock(duplicate)) return false;
                            created.Add(layout.BlockCount - 1);
                        }

                        return true;
                    }))
            {
                return;
            }

            FanlightLayoutSelection.SetIndices(layout, created);
        }

        internal static void Delete(FanlightLayoutAsset layout)
        {
            var selected = GetSelected(layout);
            if (selected.Count == 0 || layout.BlockCount <= selected.Count) return;

            var first = selected[0];
            if (FanlightLayoutEditSession.ApplyTopologyChange(
                    layout,
                    "Delete Fanlight Blocks",
                    () => layout.RemoveBlocks(selected)))
            {
                FanlightLayoutSelection.SetOnly(layout, Mathf.Min(first, layout.BlockCount - 1));
            }
        }

        internal static void Mirror(FanlightLayoutAsset layout)
        {
            var selected = GetSelected(layout);
            if (selected.Count == 0) return;

            FanlightLayoutEditSession.ApplyTopologyChange(
                layout,
                "Mirror Fanlight Blocks",
                () =>
                {
                    for (var i = 0; i < selected.Count; i++)
                    {
                        var index = selected[i];
                        var block = layout.GetBlock(index);
                        var placement = block.Placement;
                        var rows = block.CopyRows();
                        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                        {
                            var row = rows[rowIndex];
                            var left = ReflectLayoutX(placement.position + placement.Rotation * row.RightPoint);
                            var control = ReflectLayoutX(placement.position + placement.Rotation * row.ControlPoint);
                            var right = ReflectLayoutX(placement.position + placement.Rotation * row.LeftPoint);
                            var stableSeatIds = row.CopyStableSeatIds();
                            Array.Reverse(stableSeatIds);
                            rows[rowIndex] = new FanlightLayoutRow(left, control, right, stableSeatIds);
                        }

                        if (!layout.SetBlockRows(index, rows)) return false;
                        layout.SetBlockPlacement(index, FanlightBlockPlacement.Identity);
                    }

                    return true;
                });
        }

        internal static void Bend(
            FanlightLayoutAsset layout,
            float bendDegrees,
            Vector3 lateralDirection,
            Vector3 bendDirection)
        {
            var selected = GetSelected(layout);
            if (selected.Count < 2 || Mathf.Abs(bendDegrees) <= 0.0001f) return;

            lateralDirection.y = 0f;
            bendDirection.y = 0f;
            if (lateralDirection.sqrMagnitude <= 0.0001f || bendDirection.sqrMagnitude <= 0.0001f) return;

            lateralDirection.Normalize();
            bendDirection -= lateralDirection * Vector3.Dot(bendDirection, lateralDirection);
            if (bendDirection.sqrMagnitude <= 0.0001f) return;

            bendDirection.Normalize();

            var session = FanlightLayoutEditSession.Get(layout);
            if (session == null) return;

            var pivot = Vector3.zero;
            var centers = new Vector3[selected.Count];
            for (var i = 0; i < centers.Length; i++)
            {
                centers[i] = session.GetBlockBounds(selected[i]).center;
                pivot += centers[i];
            }

            pivot /= centers.Length;
            var halfSpan = 0f;
            for (var i = 0; i < centers.Length; i++)
            {
                halfSpan = Mathf.Max(
                    halfSpan,
                    Mathf.Abs(Vector3.Dot(centers[i] - pivot, lateralDirection)));
            }

            if (halfSpan <= 0.0001f) return;

            var edgeRadians = bendDegrees * Mathf.Deg2Rad;
            var radius = halfSpan / edgeRadians;
            var placements = new FanlightBlockPlacement[selected.Count];
            for (var i = 0; i < placements.Length; i++)
            {
                placements[i] = layout.GetBlock(selected[i]).Placement;
                var originalCenter = centers[i];
                var blockLocalCenter = Quaternion.Inverse(placements[i].Rotation)
                                       * (originalCenter - placements[i].position);
                var lateral = Vector3.Dot(originalCenter - pivot, lateralDirection);
                var normalized = Mathf.Clamp(lateral / halfSpan, -1f, 1f);
                var angle = edgeRadians * normalized;
                var bentCenter = originalCenter
                                 + lateralDirection * (Mathf.Sin(angle) * radius - lateral)
                                 + bendDirection * ((1f - Mathf.Cos(angle)) * radius);
                placements[i].eulerRotation.y -= angle * Mathf.Rad2Deg;
                placements[i].position = bentCenter - placements[i].Rotation * blockLocalCenter;
            }

            session.SetBlockPlacements(selected, placements, "Bend Fanlight Blocks");
        }

        internal static void SnapActiveEdge(FanlightLayoutAsset layout)
        {
            var selected = GetSelected(layout);
            var active = FanlightLayoutSelection.GetActiveIndex(layout);
            var session = FanlightLayoutEditSession.Get(layout);
            if (active < 0 || session == null) return;

            var activeCorners = session.GetCorners(active);
            var bestDelta = Vector3.zero;
            var bestDistance = float.PositiveInfinity;
            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                if (blockIndex == active || selected.Contains(blockIndex)) continue;

                var targetCorners = session.GetCorners(blockIndex);
                for (var sourceIndex = 0; sourceIndex < activeCorners.Length; sourceIndex++)
                {
                    for (var targetIndex = 0; targetIndex < targetCorners.Length; targetIndex++)
                    {
                        var delta = targetCorners[targetIndex] - activeCorners[sourceIndex];
                        var distance = delta.sqrMagnitude;
                        if (distance >= bestDistance) continue;

                        bestDistance = distance;
                        bestDelta = delta;
                    }
                }
            }

            if (!float.IsFinite(bestDistance)) return;
            var placements = new FanlightBlockPlacement[selected.Count];
            for (var i = 0; i < placements.Length; i++)
            {
                placements[i] = layout.GetBlock(selected[i]).Placement;
                placements[i].position += bestDelta;
            }

            session.SetBlockPlacements(selected, placements, "Snap Fanlight Blocks Edge");
        }

        internal static void Align(FanlightLayoutAsset layout, bool xAxis)
        {
            var selected = GetSelected(layout);
            var active = FanlightLayoutSelection.GetActiveIndex(layout);
            var session = FanlightLayoutEditSession.Get(layout);
            if (selected.Count < 2 || active < 0 || session == null) return;

            var target = session.GetBlockBounds(active).center;
            var placements = new FanlightBlockPlacement[selected.Count];
            for (var i = 0; i < placements.Length; i++)
            {
                var index = selected[i];
                placements[i] = layout.GetBlock(index).Placement;
                var center = session.GetBlockBounds(index).center;
                if (xAxis) placements[i].position.x += target.x - center.x;
                else placements[i].position.z += target.z - center.z;
            }

            session.SetBlockPlacements(selected, placements, xAxis
                ? "Align Fanlight Blocks X"
                : "Align Fanlight Blocks Z");
        }

        internal static void Distribute(FanlightLayoutAsset layout, bool xAxis)
        {
            var selected = GetSelected(layout);
            var session = FanlightLayoutEditSession.Get(layout);
            if (selected.Count < 3 || session == null) return;

            var sorted = selected.ToArray();
            Array.Sort(sorted, (a, b) => GetAxis(session.GetBlockBounds(a).center, xAxis)
                .CompareTo(GetAxis(session.GetBlockBounds(b).center, xAxis)));
            var first = GetAxis(session.GetBlockBounds(sorted[0]).center, xAxis);
            var last = GetAxis(session.GetBlockBounds(sorted[sorted.Length - 1]).center, xAxis);
            var placements = new FanlightBlockPlacement[sorted.Length];
            for (var i = 0; i < sorted.Length; i++)
            {
                placements[i] = layout.GetBlock(sorted[i]).Placement;
                var center = session.GetBlockBounds(sorted[i]).center;
                var target = Mathf.Lerp(first, last, (float)i / (sorted.Length - 1));
                if (xAxis) placements[i].position.x += target - center.x;
                else placements[i].position.z += target - center.z;
            }

            session.SetBlockPlacements(sorted, placements, xAxis
                ? "Distribute Fanlight Blocks X"
                : "Distribute Fanlight Blocks Z");
        }

        internal static void ResetPlacement(FanlightLayoutAsset layout)
        {
            var selected = GetSelected(layout);
            if (selected.Count == 0) return;

            var placements = new FanlightBlockPlacement[selected.Count];
            for (var i = 0; i < placements.Length; i++) placements[i] = FanlightBlockPlacement.Identity;
            FanlightLayoutEditSession.Get(layout)?.SetBlockPlacements(
                selected,
                placements,
                "Reset Fanlight Block Placement");
        }


        private static List<int> GetSelected(FanlightLayoutAsset layout)
        {
            var selected = new List<int>();
            FanlightLayoutSelection.GetIndices(layout, selected);
            return selected;
        }

        private static Vector3 ReflectLayoutX(Vector3 point)
            => new(-point.x, point.y, point.z);

        private static float GetAxis(Vector3 value, bool xAxis)
            => xAxis ? value.x : value.z;
    }
}
