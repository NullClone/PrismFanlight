using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class PrismFanlightScenePreview
    {
        private static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        private static readonly Color SelectedBlockColor = new(1.0f, 0.82f, 0.2f, 0.95f);
        private static readonly Color CulledBlockColor = new(1.0f, 0.25f, 0.15f, 0.45f);

        public static bool EditBlockTransforms { get; set; } = true;

        public static int SelectedBlockIndex { get; set; } = -1;


        // Methods

        public void Draw(PrismFanlight fanlight)
        {
            if (fanlight == null) return;

            var audience = fanlight.GetSeatLayout();

            if (audience.TotalSeatCount <= 0 || audience.BlockSeatCount <= 0) return;

            var targetTransform = fanlight.transform;
            var culling = BlockCullingPreview.Create(fanlight, targetTransform);

            DrawBlocks(targetTransform, audience, culling);
            DrawBlockTransformEditor(fanlight, targetTransform, audience);
        }


        private static void DrawBlocks(Transform transform, SeatLayout audience, BlockCullingPreview culling)
        {
            for (var bx = 0; bx < audience.blockCount.x; bx++)
            {
                for (var by = 0; by < audience.blockCount.y; by++)
                {
                    var block = math.int2(bx, by);
                    var blockIndex = audience.GetBlockIndex(block);
                    var isCulled = culling.IsCulled(audience, block);
                    var isSelected = blockIndex == SelectedBlockIndex;

                    Handles.color = isSelected ? SelectedBlockColor : isCulled ? CulledBlockColor : BlockColor;

                    var corners = GetBlockPlaneCorners(audience, block);
                    var p0 = transform.TransformPoint(corners[0]);
                    var p1 = transform.TransformPoint(corners[1]);
                    var p2 = transform.TransformPoint(corners[2]);
                    var p3 = transform.TransformPoint(corners[3]);

                    Handles.DrawAAPolyLine(isSelected ? 4.0f : 2.0f, p0, p1, p2, p3, p0);

                    var center = transform.TransformPoint(audience.GetBlockCenterLocal(block));
                    var handleSize = HandleUtility.GetHandleSize(center) * 0.08f;

                    if (isSelected)
                    {
                        DrawSeatDots(transform, audience, block);
                    }

                    if (!Application.isPlaying && EditBlockTransforms
                                               && Handles.Button(center, transform.rotation, handleSize, handleSize, Handles.SphereHandleCap))
                    {
                        SelectedBlockIndex = blockIndex;
                    }

                    Handles.Label(center, $"Block {bx}, {by}");
                }
            }
        }

        private static void DrawBlockTransformEditor(PrismFanlight fanlight, Transform transform, SeatLayout audience)
        {
            if (Application.isPlaying || !EditBlockTransforms) return;

            if (SelectedBlockIndex < 0 || SelectedBlockIndex >= audience.TotalBlockCount) return;

            var block = audience.GetBlockCoordinates(SelectedBlockIndex);
            var placement = audience.GetBlockTransform(block);
            var baseCenter = audience.GetBlockBaseCenterLocal(block);
            var localCenter = baseCenter + placement.position;
            var worldCenter = transform.TransformPoint(localCenter);
            var worldRotation = transform.rotation * placement.Rotation;

            EditorGUI.BeginChangeCheck();
            var newWorldCenter = Handles.PositionHandle(worldCenter, worldRotation);
            var newWorldRotation = Handles.RotationHandle(worldRotation, newWorldCenter);

            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObject(fanlight, "Edit Fanlight Block Transform");

            var serialized = new SerializedObject(fanlight);
            serialized.Update();
            var layoutProperty = serialized.FindProperty("_seatLayout");
            var transformsProperty = EnsureBlockTransforms(layoutProperty, audience.TotalBlockCount);
            var transformProperty = transformsProperty.GetArrayElementAtIndex(SelectedBlockIndex);

            transformProperty.FindPropertyRelative("position").vector3Value = transform.InverseTransformPoint(newWorldCenter) - baseCenter;
            transformProperty.FindPropertyRelative("eulerRotation").vector3Value = (Quaternion.Inverse(transform.rotation) * newWorldRotation).eulerAngles;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(fanlight);
        }

        private static void DrawSeatDots(Transform transform, SeatLayout audience, int2 block)
        {
            var color = SelectedBlockColor;
            color.a = 0.7f;
            Handles.color = color;

            for (var y = 0; y < audience.seatPerBlock.y; y++)
            {
                for (var x = 0; x < audience.seatPerBlock.x; x++)
                {
                    var seat = math.int2(x, y);
                    var world = transform.TransformPoint(audience.GetSeatLocalPosition(block, seat));
                    var size = HandleUtility.GetHandleSize(world) * 0.025f;
                    Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
                }
            }
        }

        private static SerializedProperty EnsureBlockTransforms(SerializedProperty layoutProperty, int count)
        {
            var transformsProperty = layoutProperty.FindPropertyRelative("blockTransforms");

            if (transformsProperty.arraySize != count)
            {
                var oldSize = transformsProperty.arraySize;
                transformsProperty.arraySize = count;

                for (var i = oldSize; i < count; i++)
                {
                    var element = transformsProperty.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("position").vector3Value = Vector3.zero;
                    element.FindPropertyRelative("eulerRotation").vector3Value = Vector3.zero;
                }
            }

            return transformsProperty;
        }

        private static Vector3[] GetBlockPlaneCorners(SeatLayout audience, int2 block)
        {
            var min = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
            var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;

            return new[]
            {
                audience.TransformBlockPoint(block, new Vector3(min.x, 0f, min.y)),
                audience.TransformBlockPoint(block, new Vector3(max.x, 0f, min.y)),
                audience.TransformBlockPoint(block, new Vector3(max.x, 0f, max.y)),
                audience.TransformBlockPoint(block, new Vector3(min.x, 0f, max.y))
            };
        }
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

            return (bounds.center, bounds.extents.magnitude + 4.0f);
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
