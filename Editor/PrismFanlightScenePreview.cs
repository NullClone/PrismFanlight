using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class PrismFanlightScenePreview
    {
        private const int MaxPreviewSeats = 12000;

        private static readonly Color SeatColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        private static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.35f);
        private static readonly Color OriginColor = new(1.0f, 0.9f, 0.25f, 0.85f);

        public int PreviewSeatLimit => MaxPreviewSeats;

        public void Draw(PrismFanlight fanlight)
        {
            if (fanlight == null) return;

            var audience = fanlight.GetAudience();
            if (audience.TotalSeatCount <= 0 || audience.BlockSeatCount <= 0) return;

            var targetTransform = fanlight.transform;
            DrawBlocks(targetTransform, audience);
            DrawSeats(targetTransform, audience);
            DrawOrigin(targetTransform);
        }

        private static void DrawBlocks(Transform transform, Audience audience)
        {
            Handles.color = BlockColor;

            for (var bx = 0; bx < audience.blockCount.x; bx++)
            {
                for (var by = 0; by < audience.blockCount.y; by++)
                {
                    var block = math.int2(bx, by);
                    var min = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
                    var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;

                    var p0 = ToWorld(transform, math.float2(min.x, min.y));
                    var p1 = ToWorld(transform, math.float2(max.x, min.y));
                    var p2 = ToWorld(transform, math.float2(max.x, max.y));
                    var p3 = ToWorld(transform, math.float2(min.x, max.y));

                    Handles.DrawAAPolyLine(2.0f, p0, p1, p2, p3, p0);
                }
            }
        }

        private static void DrawSeats(Transform transform, Audience audience)
        {
            Handles.color = SeatColor;

            var previewCount = Mathf.Min(audience.TotalSeatCount, MaxPreviewSeats);
            var step = Mathf.Max(1, Mathf.CeilToInt((float)audience.TotalSeatCount / previewCount));

            for (var i = 0; i < audience.TotalSeatCount; i += step)
            {
                var (block, seat) = audience.GetCoordinatesFromIndex(i);
                var pos = audience.GetPositionOnPlane(block, seat);
                var world = ToWorld(transform, pos);
                var size = HandleUtility.GetHandleSize(world) * 0.025f;

                Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
            }
        }

        private static void DrawOrigin(Transform transform)
        {
            Handles.color = OriginColor;
            var size = HandleUtility.GetHandleSize(transform.position) * 0.12f;
            Handles.SphereHandleCap(0, transform.position, Quaternion.identity, size, EventType.Repaint);
        }

        private static Vector3 ToWorld(Transform transform, float2 planePosition)
            => transform.TransformPoint(new Vector3(planePosition.x, 0.0f, planePosition.y));
    }
}
