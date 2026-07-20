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
