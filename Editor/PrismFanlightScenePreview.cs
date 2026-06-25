using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class PrismFanlightScenePreview
    {
        private static readonly Color SeatColor = new(1.0f, 0.65f, 0.0f, 0.75f);
        private static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        private static readonly Color CulledBlockColor = new(1.0f, 0.25f, 0.15f, 0.45f);


        // Methods

        public void Draw(PrismFanlight fanlight)
        {
            if (fanlight == null) return;

            var audience = fanlight.GetSeatLayout();
            if (audience.TotalSeatCount <= 0 || audience.BlockSeatCount <= 0) return;

            var targetTransform = fanlight.transform;
            var previewBlock = GetPreviewBlock(audience);
            var culling = BlockCullingPreview.Create(fanlight, targetTransform);

            DrawBlocks(targetTransform, audience, culling);
            DrawSeatsInBlock(targetTransform, audience, previewBlock);
        }


        private static void DrawBlocks(Transform transform, SeatLayout audience, BlockCullingPreview culling)
        {
            for (var bx = 0; bx < audience.blockCount.x; bx++)
            {
                for (var by = 0; by < audience.blockCount.y; by++)
                {
                    var block = math.int2(bx, by);
                    var isCulled = culling.IsCulled(audience, block);

                    Handles.color = isCulled ? CulledBlockColor : BlockColor;

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

        private static void DrawSeatsInBlock(Transform transform, SeatLayout audience, int2 block)
        {
            Handles.color = SeatColor;

            for (var y = 0; y < audience.seatPerBlock.y; y++)
            {
                for (var x = 0; x < audience.seatPerBlock.x; x++)
                {
                    var seat = math.int2(x, y);
                    var pos = audience.GetPositionOnPlane(block, seat);
                    var world = ToWorld(transform, pos);
                    var size = HandleUtility.GetHandleSize(world) * 0.025f;

                    Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
                }
            }
        }

        private static int2 GetPreviewBlock(SeatLayout audience)
        {
            return math.max((audience.blockCount - math.int2(1, 1)) / 2, math.int2(0, 0));
        }

        private static Vector3 ToWorld(Transform transform, float2 planePosition) => transform.TransformPoint(new Vector3(planePosition.x, 0.0f, planePosition.y));
    }

    internal readonly struct BlockCullingPreview
    {
        private readonly bool _enabled;
        private readonly Transform _transform;
        private readonly Plane[] _planes;
        private readonly float _scale;


        private BlockCullingPreview(bool enabled, Transform transform, Plane[] planes)
        {
            _enabled = enabled;
            _transform = transform;
            _planes = planes;
            _scale = GetMaxScale(transform.localToWorldMatrix);
        }

        public static BlockCullingPreview Create(PrismFanlight fanlight, Transform transform)
        {
            if (!fanlight.IsCullingEnabled || fanlight.CullingCamera == null)
            {
                return new BlockCullingPreview(false, transform, null);
            }

            return new BlockCullingPreview(true, transform, GeometryUtility.CalculateFrustumPlanes(fanlight.CullingCamera));
        }

        public bool IsCulled(SeatLayout audience, int2 block)
        {
            if (!_enabled || _planes == null) return false;

            var (center, radius) = GetBlockSphere(audience, block);
            var worldCenter = _transform.TransformPoint(center);
            var worldRadius = radius * _scale;

            for (var i = 0; i < _planes.Length; i++)
            {
                if (_planes[i].GetDistanceToPoint(worldCenter) < -worldRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static (Vector3 center, float radius) GetBlockSphere(SeatLayout audience, int2 block)
        {
            var min = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
            var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;
            var center2 = (min + max) * 0.5f;
            var size2 = math.max(max - min, math.float2(0.01f, 0.01f));
            var radius = math.length(math.float3(size2.x, 8.0f, size2.y) * 0.5f) + 4.0f;

            return (new Vector3(center2.x, 0.0f, center2.y), radius);
        }

        private static float GetMaxScale(Matrix4x4 matrix)
        {
            var x = matrix.MultiplyVector(Vector3.right).magnitude;
            var y = matrix.MultiplyVector(Vector3.up).magnitude;
            var z = matrix.MultiplyVector(Vector3.forward).magnitude;
            return Mathf.Max(x, Mathf.Max(y, z));
        }
    }
}
