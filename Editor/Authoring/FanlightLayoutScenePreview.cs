using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightLayoutScenePreview
    {
        // Fields

        private const float BlockPlanePickDistance = 4.99f;

        internal static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        internal static readonly Color SelectedColor = new(1.0f, 0.82f, 0.2f, 0.95f);
        private static readonly Dictionary<string, string> SelectedBlockIds = new(StringComparer.Ordinal);

        private readonly List<int> _visibleBlocks = new();
        private readonly Plane[] _planes = new Plane[6];


        // Properties

        internal bool IsSelected { get; private set; }


        // Methods

        internal static int GetSelectedBlockIndex(FanlightLayoutAsset layout)
        {
            if (layout == null || !SelectedBlockIds.TryGetValue(layout.LayoutId.Value, out var blockId)) return -1;

            for (var i = 0; i < layout.TotalBlockCount; i++)
            {
                if (string.Equals(layout.GetBlock(i).BlockId, blockId, StringComparison.Ordinal)) return i;
            }

            return -1;
        }

        internal bool Draw(PrismFanlight fanlight)
        {
            var layout = fanlight?.LayoutAsset;

            if (layout == null || !layout.IsInitialized) return fanlight;

            if (FanlightLayoutIdRegistry.IsDuplicate(layout))
            {
                fanlight.SetEditorLayoutBlocked(true);
                return false;
            }

            fanlight.SetEditorLayoutBlocked(false);

            var session = FanlightLayoutEditSession.Get(layout);

            if (session == null) return false;

            if (fanlight.EditorPreviewContentHash != session.RuntimeLayout.ContentHash)
            {
                fanlight.SetEditorLayoutPreview(session.RuntimeLayout, -1);

                EditorApplication.QueuePlayerLoopUpdate();
            }

            var sceneView = SceneView.currentDrawingSceneView;
            var camera = sceneView != null ? sceneView.camera : null;
            if (camera != null)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, _planes);

                session.QueryVisible(_planes, fanlight.transform.localToWorldMatrix, _visibleBlocks);
            }
            else
            {
                _visibleBlocks.Clear();

                for (var i = 0; i < layout.TotalBlockCount; i++)
                {
                    _visibleBlocks.Add(i);
                }
            }

            var selected = GetSelectedBlockIndex(layout);
            var transform = fanlight.transform;

            foreach (var blockIndex in _visibleBlocks)
            {
                var corners = session.GetCorners(blockIndex);
                var p0 = transform.TransformPoint(corners[0]);
                var p1 = transform.TransformPoint(corners[1]);
                var p2 = transform.TransformPoint(corners[2]);
                var p3 = transform.TransformPoint(corners[3]);
                var isSelected = blockIndex == selected;
                var controlId = GUIUtility.GetControlID(FocusType.Passive);

                if (!Application.isPlaying && DoBlockButton(controlId, p0, p1, p2, p3, isSelected))
                {
                    SelectedBlockIds[layout.LayoutId.Value] = layout.GetBlock(blockIndex).BlockId;

                    selected = blockIndex;
                    isSelected = true;

                    SceneView.RepaintAll();
                }

                Handles.color = isSelected ? SelectedColor : BlockColor;
                Handles.DrawAAPolyLine(isSelected ? 4f : 2f, p0, p1, p2, p3, p0);
            }

            if (selected >= 0)
            {
                DrawSelectedSeatDots(transform, session, selected);
                DrawTransformHandle(fanlight, layout, session, selected);
                return true;
            }

            return false;
        }

        internal void ResetSelected(FanlightLayoutAsset layout)
        {
            var index = GetSelectedBlockIndex(layout);

            if (index < 0) return;

            FanlightLayoutEditSession.Get(layout)?.SetBlockPlacement(index, FanlightBlockPlacement.Identity, "Reset Fanlight Block Placement");
        }


        private static bool DoBlockButton(int controlId, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool isSelected)
        {
            var current = Event.current;

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.Layout:
                    if (GUI.enabled && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == controlId))
                    {
                        HandleUtility.AddControl(controlId, DistanceToBlock(p0, p1, p2, p3));
                    }

                    break;
                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == controlId)
                    {
                        HandleUtility.Repaint();
                    }

                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlId && current.button == 0 && !current.alt)
                    {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && current.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        return HandleUtility.nearestControl == controlId;
                    }

                    break;
                case EventType.Repaint:
                    if (HandleUtility.nearestControl == controlId
                        && GUI.enabled
                        && GUIUtility.hotControl == 0
                        && !current.alt)
                    {
                        var previousColor = Handles.color;
                        var highlight = isSelected ? SelectedColor : BlockColor;
                        highlight.a = 0.18f;
                        Handles.color = highlight;
                        Handles.DrawAAConvexPolygon(p0, p1, p2, p3);
                        Handles.color = previousColor;
                    }

                    break;
            }

            return false;
        }

        private static float DistanceToBlock(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var point = Event.current.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(point);
            var plane = new Plane(p0, p1, p2);

            if (plane.Raycast(ray, out var distance) && distance >= 0f)
            {
                var hit = ray.GetPoint(distance);

                if (IsPointInTriangle(hit, p0, p1, p2) || IsPointInTriangle(hit, p0, p2, p3))
                {
                    return BlockPlanePickDistance;
                }
            }

            return float.PositiveInfinity;
        }

        private static bool IsPointInTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            var edgeA = b - a;
            var edgeB = c - a;
            var offset = point - a;
            var dotAA = Vector3.Dot(edgeA, edgeA);
            var dotAB = Vector3.Dot(edgeA, edgeB);
            var dotBB = Vector3.Dot(edgeB, edgeB);
            var dotAP = Vector3.Dot(edgeA, offset);
            var dotBP = Vector3.Dot(edgeB, offset);
            var denominator = dotAA * dotBB - dotAB * dotAB;

            if (Mathf.Abs(denominator) <= 0.000001f) return false;

            var inverse = 1f / denominator;
            var u = (dotBB * dotAP - dotAB * dotBP) * inverse;
            var v = (dotAA * dotBP - dotAB * dotAP) * inverse;
            return u >= 0f && v >= 0f && u + v <= 1f;
        }

        private static void DrawTransformHandle(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            int blockIndex)
        {
            if (Application.isPlaying) return;

            var block = layout.GetBlockCoordinates(blockIndex);
            var placement = layout.GetBlock(blockIndex).Placement;
            var baseCenter = layout.GetBlockBaseCenterLocal(block);
            var worldCenter = fanlight.transform.TransformPoint(baseCenter + placement.position);
            var worldRotation = fanlight.transform.rotation * placement.Rotation;
            var handleRotation = Tools.pivotRotation == PivotRotation.Local ? worldRotation : Quaternion.identity;

            EditorGUI.BeginChangeCheck();

            var nextCenter = Handles.PositionHandle(worldCenter, handleRotation);
            var nextRotation = Handles.RotationHandle(worldRotation, nextCenter);

            if (!EditorGUI.EndChangeCheck()) return;

            var next = new FanlightBlockPlacement
            {
                position = fanlight.transform.InverseTransformPoint(nextCenter) - baseCenter,
                eulerRotation = (Quaternion.Inverse(fanlight.transform.rotation) * nextRotation).eulerAngles
            };

            session.SetBlockPlacement(blockIndex, next, "Edit Fanlight Block Placement");
        }

        private static void DrawSelectedSeatDots(Transform transform, FanlightLayoutEditSession session, int blockIndex)
        {
            var block = session.RuntimeLayout.Blocks[blockIndex];
            var end = block.startIndex + block.count;
            var color = SelectedColor;
            color.a = 0.7f;
            Handles.color = color;

            for (var i = block.startIndex; i < end; i++)
            {
                var packed = session.RuntimeLayout.Seats[i].localPositionSeed;
                var world = transform.TransformPoint(new Vector3(packed.x, packed.y, packed.z));
                var size = HandleUtility.GetHandleSize(world) * 0.025f;
                Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
            }
        }
    }
}
