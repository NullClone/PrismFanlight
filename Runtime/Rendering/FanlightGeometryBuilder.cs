using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal static class FanlightGeometryBuilder
    {
        private static Mesh _audienceQuad;

        public static Mesh GetAudienceQuad()
        {
            if (_audienceQuad != null)
            {
                return _audienceQuad;
            }

            _audienceQuad = new Mesh
            {
                name = "PrismFanlightAudienceQuad",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, 0f),
                    new Vector3(0.5f, 0f, 0f),
                    new Vector3(-0.5f, 1f, 0f),
                    new Vector3(0.5f, 1f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                }
            };

            _audienceQuad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            _audienceQuad.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            return _audienceQuad;
        }

        public static FanlightSeatData[] BuildSeatData(SeatLayout audience)
        {
            var data = new FanlightSeatData[audience.TotalSeatCount];

            for (var i = 0; i < data.Length; i++)
            {
                var (block, seat) = audience.GetCoordinatesFromIndex(i);
                var planePosition = audience.GetPositionOnPlane(block, seat);
                var localPosition = new Vector3(planePosition.x, 0.0f, planePosition.y);

                data[i] = new FanlightSeatData(
                    localPosition,
                    new Vector2(planePosition.x, planePosition.y),
                    new Vector2(block.x, block.y),
                    (uint)i * 2u + 123u);
            }

            return data;
        }

        public static FanlightBlockData[] BuildBlockData(SeatLayout audience, Mesh mesh)
        {
            var data = new FanlightBlockData[audience.blockCount.x * audience.blockCount.y];
            var blockSeatCount = audience.BlockSeatCount;
            var meshPadding = mesh.bounds.size.magnitude + 4.0f;

            for (var by = 0; by < audience.blockCount.y; by++)
            {
                for (var bx = 0; bx < audience.blockCount.x; bx++)
                {
                    var block = math.int2(bx, by);
                    var min = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
                    var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;
                    var center2 = (min + max) * 0.5f;
                    var size2 = math.max(max - min, math.float2(0.01f, 0.01f));
                    var radius = math.length(math.float3(size2.x, 8.0f, size2.y) * 0.5f) + meshPadding;
                    var blockIndex = by * audience.blockCount.x + bx;

                    data[blockIndex] = new FanlightBlockData(
                        new Vector3(center2.x, 0.0f, center2.y),
                        radius,
                        blockIndex * blockSeatCount,
                        blockSeatCount);
                }
            }

            return data;
        }

        public static Bounds BuildBounds(SeatLayout audience, Mesh mesh)
        {
            var min = new float2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new float2(float.NegativeInfinity, float.NegativeInfinity);

            for (var bx = 0; bx < audience.blockCount.x; bx++)
            {
                for (var by = 0; by < audience.blockCount.y; by++)
                {
                    var block = math.int2(bx, by);
                    min = math.min(min, audience.GetPositionOnPlane(block, math.int2(0, 0)));
                    max = math.max(max, audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)));
                }
            }

            var center = new Vector3((min.x + max.x) * 0.5f, 0.0f, (min.y + max.y) * 0.5f);
            var size = new Vector3(math.max(max.x - min.x, 1.0f), 8.0f, math.max(max.y - min.y, 1.0f));
            var meshPadding = mesh.bounds.size.magnitude + 4.0f;
            size += Vector3.one * meshPadding;
            return new Bounds(center, size);
        }

        public static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            var center = matrix.MultiplyPoint3x4(bounds.center);
            var extents = bounds.extents;

            var axisX = matrix.MultiplyVector(new Vector3(extents.x, 0.0f, 0.0f));
            var axisY = matrix.MultiplyVector(new Vector3(0.0f, extents.y, 0.0f));
            var axisZ = matrix.MultiplyVector(new Vector3(0.0f, 0.0f, extents.z));

            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds(center, extents * 2.0f);
        }

        public static float GetMaxScale(Matrix4x4 matrix)
        {
            var x = matrix.MultiplyVector(Vector3.right).magnitude;
            var y = matrix.MultiplyVector(Vector3.up).magnitude;
            var z = matrix.MultiplyVector(Vector3.forward).magnitude;
            return Mathf.Max(x, Mathf.Max(y, z));
        }
    }
}
