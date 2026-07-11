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

        public static FanlightSeatData[] BuildSeatData(SeatLayout audience, bool useBakedData = true)
        {
            if (useBakedData && audience.TryGetBakedSeats(out var bakedSeats))
            {
                return bakedSeats;
            }

            var data = new FanlightSeatData[audience.TotalSeatCount];

            for (var i = 0; i < data.Length; i++)
            {
                var (block, seat) = audience.GetCoordinatesFromIndex(i);
                var planePosition = audience.GetPositionOnPlane(block, seat);
                var localPosition = audience.GetSeatLocalPosition(block, seat);

                data[i] = new FanlightSeatData(
                    localPosition,
                    new Vector2(planePosition.x, planePosition.y),
                    new Vector2(block.x, block.y),
                    (uint)i);
            }

            return data;
        }

        public static FanlightBakedBlockData[] BuildBakedBlockData(SeatLayout audience)
        {
            var data = new FanlightBakedBlockData[audience.TotalBlockCount];
            var blockSeatCount = audience.BlockSeatCount;

            for (var by = 0; by < audience.blockCount.y; by++)
            {
                for (var bx = 0; bx < audience.blockCount.x; bx++)
                {
                    var block = math.int2(bx, by);
                    var bounds = BuildBlockAuthoringBounds(audience, block);
                    var blockIndex = audience.GetBlockIndex(block);

                    data[blockIndex] = new FanlightBakedBlockData(
                        bounds.center,
                        bounds.extents.magnitude,
                        blockIndex * blockSeatCount,
                        blockSeatCount);
                }
            }

            return data;
        }

        public static FanlightBlockData[] BuildBlockData(SeatLayout audience, Mesh mesh, bool useBakedData = true)
        {
            var bakedBlocks = useBakedData && audience.TryGetBakedBlocks(out var blocks)
                ? blocks
                : BuildBakedBlockData(audience);
            var data = new FanlightBlockData[bakedBlocks.Length];
            var meshPadding = mesh.bounds.size.magnitude + 4.0f;

            for (var i = 0; i < bakedBlocks.Length; i++)
            {
                var block = bakedBlocks[i];
                data[i] = new FanlightBlockData(
                    block.localCenter,
                    block.radius + meshPadding,
                    block.startIndex,
                    block.count);
            }

            return data;
        }

        public static Bounds BuildBounds(SeatLayout audience, Mesh mesh, bool useBakedData = true)
        {
            var bounds = useBakedData && audience.TryGetBakedBounds(out var bakedBounds)
                ? bakedBounds
                : BuildAuthoringBounds(audience);

            bounds.Expand(mesh.bounds.size.magnitude + 4.0f);
            return bounds;
        }

        public static Bounds BuildAuthoringBounds(SeatLayout audience)
        {
            var hasBounds = false;
            var bounds = default(Bounds);

            for (var by = 0; by < audience.blockCount.y; by++)
            {
                for (var bx = 0; bx < audience.blockCount.x; bx++)
                {
                    var blockBounds = BuildBlockAuthoringBounds(audience, math.int2(bx, by));

                    if (!hasBounds)
                    {
                        bounds = blockBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(blockBounds.min);
                        bounds.Encapsulate(blockBounds.max);
                    }
                }
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        public static Bounds BuildBlockAuthoringBounds(SeatLayout audience, int2 block)
        {
            var min2 = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
            var max2 = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;
            var min = new Vector3(min2.x, -4f, min2.y);
            var max = new Vector3(max2.x, 4f, max2.y);
            var first = audience.TransformBlockPoint(block, new Vector3(min.x, min.y, min.z));
            var bounds = new Bounds(first, Vector3.zero);

            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(max.x, min.y, min.z)));
            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(min.x, max.y, min.z)));
            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(max.x, max.y, min.z)));
            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(min.x, min.y, max.z)));
            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(max.x, min.y, max.z)));
            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(min.x, max.y, max.z)));
            bounds.Encapsulate(audience.TransformBlockPoint(block, new Vector3(max.x, max.y, max.z)));
            return bounds;
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
