using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    public static class PrismFanlightGizmoUtility
    {
        public static void DrawWireArrow(Vector3 pos, Quaternion rot, Vector3 size, bool cross = false)
        {
            Handles.matrix = Matrix4x4.TRS(pos, rot, size);

            var points = new Vector3[]
            {
                new Vector3(0.0f, 0.0f, -1.0f),
                new Vector3(0.0f, 0.5f, -1.0f),
                new Vector3(0.0f, 0.5f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
            };

            var addAngle = cross ? 90.0f : 180.0f;
            var loop = cross ? 4 : 2;

            for (int j = 0; j < loop; j++)
            {
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Handles.DrawLine(points[i], points[i + 1]);
                }

                rot *= Quaternion.AngleAxis(addAngle, Vector3.forward);
                Handles.matrix = Matrix4x4.TRS(pos, rot, size);
            }

            Handles.matrix = Matrix4x4.identity;
        }
    }
}
