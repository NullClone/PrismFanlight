using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutShapeUtility
    {
        // Methods

        internal static FanlightLayoutRow[] CloneRows(FanlightLayoutBlock block)
        {
            var rows = new FanlightLayoutRow[block.RowCount];
            for (var i = 0; i < rows.Length; i++)
            {
                var row = block.GetRow(i);
                rows[i] = new FanlightLayoutRow(
                    row.LeftPoint,
                    row.ControlPoint,
                    row.RightPoint,
                    row.CopyStableSeatIds());
            }

            return rows;
        }

        internal static int GetHandleCount(int rowCount) => rowCount == 1 ? 3 : 6;

        internal static Vector3 GetHandleBlockPoint(int handle, FanlightLayoutBlock block)
        {
            var first = block.GetRow(0);
            if (block.RowCount == 1)
            {
                return handle switch
                {
                    0 => first.LeftPoint,
                    1 => first.RightPoint,
                    _ => first.ControlPoint
                };
            }

            var last = block.GetRow(block.RowCount - 1);
            return GetHandleBlockPoint(handle, first, last);
        }

        internal static Vector3 GetHandleBlockPoint(int handle, FanlightLayoutRow[] rows)
        {
            var first = rows[0];
            if (rows.Length == 1)
            {
                return handle switch
                {
                    0 => first.LeftPoint,
                    1 => first.RightPoint,
                    _ => first.ControlPoint
                };
            }

            return GetHandleBlockPoint(handle, first, rows[^1]);
        }

        internal static FanlightLayoutRow[] CreateRows(
            FanlightLayoutRow[] sourceRows,
            int handle,
            Vector3 targetPoint)
        {
            if (sourceRows.Length == 1)
            {
                var source = sourceRows[0];
                var left = source.LeftPoint;
                var control = source.ControlPoint;
                var right = source.RightPoint;
                targetPoint.y = GetHandleBlockPoint(handle, sourceRows).y;

                if (handle == 0)
                {
                    left = targetPoint;
                }
                else if (handle == 1)
                {
                    right = targetPoint;
                }
                else
                {
                    control = targetPoint;
                }

                return new[]
                {
                    new FanlightLayoutRow(left, control, right, source.CopyStableSeatIds())
                };
            }

            var first = sourceRows[0];
            var last = sourceRows[^1];
            var cage = new[] { first.LeftPoint, first.RightPoint, last.RightPoint, last.LeftPoint };
            var frontControl = first.ControlPoint;
            var backControl = last.ControlPoint;

            targetPoint.y = GetHandleBlockPoint(handle, sourceRows).y;
            if (handle < 4)
            {
                cage[handle] = targetPoint;
            }
            else if (handle == 4)
            {
                frontControl = targetPoint;
            }
            else
            {
                backControl = targetPoint;
            }

            var rows = new FanlightLayoutRow[sourceRows.Length];
            var frontBulge = frontControl - (cage[0] + cage[1]) * 0.5f;
            var backBulge = backControl - (cage[3] + cage[2]) * 0.5f;
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var t = (float)rowIndex / (rows.Length - 1);
                var source = sourceRows[rowIndex];
                var left = Vector3.Lerp(cage[0], cage[3], t);
                var right = Vector3.Lerp(cage[1], cage[2], t);
                var control = (left + right) * 0.5f + Vector3.Lerp(frontBulge, backBulge, t);
                rows[rowIndex] = new FanlightLayoutRow(left, control, right, source.CopyStableSeatIds());
            }

            return rows;
        }

        private static Vector3 GetHandleBlockPoint(
            int handle,
            FanlightLayoutRow first,
            FanlightLayoutRow last)
            => handle switch
            {
                0 => first.LeftPoint,
                1 => first.RightPoint,
                2 => last.RightPoint,
                3 => last.LeftPoint,
                4 => first.ControlPoint,
                _ => last.ControlPoint
            };
    }
}
