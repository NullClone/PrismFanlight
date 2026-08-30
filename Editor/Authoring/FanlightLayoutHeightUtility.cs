using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutHeightUtility
    {
        // Methods

        internal static bool AddRise(
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> blockIndices,
            float riseDelta)
        {
            if (layout == null
                || session == null
                || blockIndices == null
                || blockIndices.Count == 0
                || !float.IsFinite(riseDelta)
                || Mathf.Approximately(riseDelta, 0f))
            {
                return false;
            }

            var deltas = new float[blockIndices.Count];
            for (var i = 0; i < deltas.Length; i++) deltas[i] = riseDelta;
            return ApplyRiseDeltas(layout, session, blockIndices, deltas, "Raise Fanlight Block Backs");
        }

        private static bool ApplyRiseDeltas(
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> blockIndices,
            IReadOnlyList<float> riseDeltas,
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
                    var offset = Vector3.up * (riseDeltas[selectionIndex] * t / upY);
                    rows[rowIndex] = new FanlightLayoutRow(
                        source.LeftPoint + offset,
                        source.ControlPoint + offset,
                        source.RightPoint + offset,
                        source.CopyStableSeatIds());
                }

                rowSets[selectionIndex] = rows;
            }

            return session.SetBlockRows(blockIndices, rowSets, undoName);
        }

    }
}
