using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutHeightUtility
    {
        internal enum Edge
        {
            Front,
            Right,
            Back,
            Left
        }


        // Methods

        internal static Vector3 GetEdgeBlockPoint(FanlightLayoutBlock block, Edge edge)
        {
            var first = block.GetRow(0);
            var last = block.GetRow(block.RowCount - 1);
            return edge switch
            {
                Edge.Front => EvaluateQuadratic(first, 0.5f),
                Edge.Right => (first.RightPoint + last.RightPoint) * 0.5f,
                Edge.Back => EvaluateQuadratic(last, 0.5f),
                _ => (first.LeftPoint + last.LeftPoint) * 0.5f
            };
        }

        internal static Vector3 GetEdgeOutwardBlockDirection(FanlightLayoutBlock block, Edge edge)
        {
            var front = GetEdgeBlockPoint(block, Edge.Front);
            var right = GetEdgeBlockPoint(block, Edge.Right);
            var back = GetEdgeBlockPoint(block, Edge.Back);
            var left = GetEdgeBlockPoint(block, Edge.Left);
            var center = (front + right + back + left) * 0.25f;
            var direction = GetEdgeBlockPoint(block, edge) - center;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f) return direction.normalized;

            return edge switch
            {
                Edge.Front => Vector3.back,
                Edge.Right => Vector3.right,
                Edge.Back => Vector3.forward,
                _ => Vector3.left
            };
        }

        internal static bool AddEdgeHeight(
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> blockIndices,
            Edge edge,
            float heightDelta)
        {
            if (layout == null
                || session == null
                || blockIndices == null
                || blockIndices.Count == 0
                || !float.IsFinite(heightDelta)
                || Mathf.Approximately(heightDelta, 0f))
            {
                return false;
            }

            var editableBlocks = new List<int>();
            for (var i = 0; i < blockIndices.Count; i++)
            {
                var blockIndex = blockIndices[i];
                if (layout.GetBlock(blockIndex).RowCount >= 2) editableBlocks.Add(blockIndex);
            }

            if (editableBlocks.Count == 0) return false;

            var deltas = new float[editableBlocks.Count];
            for (var i = 0; i < deltas.Length; i++) deltas[i] = heightDelta;
            return ApplyHeightDeltas(layout, session, editableBlocks, edge, deltas, "Adjust Fanlight Block Height");
        }

        private static bool ApplyHeightDeltas(
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> blockIndices,
            Edge edge,
            IReadOnlyList<float> heightDeltas,
            string undoName)
        {
            var rowSets = new FanlightLayoutRow[blockIndices.Count][];
            for (var selectionIndex = 0; selectionIndex < blockIndices.Count; selectionIndex++)
            {
                var block = layout.GetBlock(blockIndices[selectionIndex]);
                var placement = block.Placement;
                var upY = Vector3.Dot(placement.Rotation * Vector3.up, Vector3.up);
                if (Mathf.Abs(upY) <= 0.0001f) return false;

                var rows = block.CopyRows();
                for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    var source = rows[rowIndex];
                    var t = rows.Length == 1 ? 0f : (float)rowIndex / (rows.Length - 1);
                    var height = heightDeltas[selectionIndex] / upY;
                    var leftOffset = Vector3.up * (height * GetHeightWeight(edge, t, 0));
                    var controlOffset = Vector3.up * (height * GetHeightWeight(edge, t, 1));
                    var rightOffset = Vector3.up * (height * GetHeightWeight(edge, t, 2));
                    rows[rowIndex] = new FanlightLayoutRow(
                        source.LeftPoint + leftOffset,
                        source.ControlPoint + controlOffset,
                        source.RightPoint + rightOffset,
                        source.CopyStableSeatIds());
                }

                rowSets[selectionIndex] = rows;
            }

            return session.SetBlockRows(blockIndices, rowSets, undoName);
        }

        private static float GetHeightWeight(Edge edge, float rowRatio, int pointIndex)
            => edge switch
            {
                Edge.Front => 1f - rowRatio,
                Edge.Back => rowRatio,
                Edge.Left => pointIndex switch
                {
                    0 => 1f,
                    1 => 0.5f,
                    _ => 0f
                },
                _ => pointIndex switch
                {
                    0 => 0f,
                    1 => 0.5f,
                    _ => 1f
                }
            };

        private static Vector3 EvaluateQuadratic(FanlightLayoutRow row, float t)
        {
            var inverse = 1f - t;
            return inverse * inverse * row.LeftPoint
                   + 2f * inverse * t * row.ControlPoint
                   + t * t * row.RightPoint;
        }

    }
}
