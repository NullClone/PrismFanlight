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
        private const int MaximumSeatDots = 512;

        internal static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        internal static readonly Color SelectedColor = new(1.0f, 0.82f, 0.2f, 0.95f);
        private readonly List<int> _visibleBlocks = new();
        private readonly List<int> _selectedBlocks = new();
        private readonly Plane[] _planes = new Plane[6];


        // Methods

        internal static int GetSelectedBlockIndex(FanlightLayoutAsset layout)
            => FanlightLayoutSelection.GetActiveIndex(layout);

        internal static void GetSelectedBlockIndices(FanlightLayoutAsset layout, List<int> results)
        {
            FanlightLayoutSelection.GetIndices(layout, results);
        }

        internal static int GetSelectedRowIndex(FanlightLayoutAsset layout)
            => FanlightLayoutSelection.GetSelectedRowIndex(layout);

        internal bool Draw(PrismFanlight fanlight)
        {
            var layout = fanlight?.LayoutAsset;
            if (layout == null || !layout.IsInitialized) return false;

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

            CollectVisibleBlocks(fanlight, layout, session);
            GetSelectedBlockIndices(layout, _selectedBlocks);

            var transform = fanlight.transform;

            foreach (var blockIndex in _visibleBlocks)
            {
                var corners = session.GetCorners(blockIndex);
                var p0 = transform.TransformPoint(corners[0]);
                var p1 = transform.TransformPoint(corners[1]);
                var p2 = transform.TransformPoint(corners[2]);
                var p3 = transform.TransformPoint(corners[3]);
                var isSelected = _selectedBlocks.Contains(blockIndex);
                var controlId = GUIUtility.GetControlID(FocusType.Passive);

                if (!Application.isPlaying && DoBlockButton(controlId, p0, p1, p2, p3, isSelected))
                {
                    FanlightLayoutSelection.Toggle(layout, blockIndex, EditorGUI.actionKey);
                    GetSelectedBlockIndices(layout, _selectedBlocks);
                    isSelected = _selectedBlocks.Contains(blockIndex);
                }

                Handles.color = isSelected ? SelectedColor : BlockColor;
                Handles.DrawAAPolyLine(isSelected ? 4f : 2f, p0, p1, p2, p3, p0);
            }

            var activeBlock = GetSelectedBlockIndex(layout);
            if (activeBlock < 0) return false;

            DrawSelectedSeatDots(transform, session, activeBlock);

            return true;
        }

        internal static bool TryGetToolContext(
            PrismFanlight fanlight,
            List<int> selectedBlocks,
            out FanlightLayoutAsset layout,
            out FanlightLayoutEditSession session,
            out int activeBlockIndex)
        {
            layout = fanlight != null ? fanlight.LayoutAsset : null;
            session = null;
            activeBlockIndex = -1;
            selectedBlocks.Clear();

            if (Application.isPlaying
                || layout == null
                || !layout.IsInitialized
                || FanlightLayoutIdRegistry.IsDuplicate(layout))
            {
                return false;
            }

            session = FanlightLayoutEditSession.Get(layout);

            if (session == null) return false;

            FanlightLayoutSelection.GetIndices(layout, selectedBlocks);

            activeBlockIndex = FanlightLayoutSelection.GetActiveIndex(layout);

            return selectedBlocks.Count > 0 && activeBlockIndex >= 0;
        }

        private void CollectVisibleBlocks(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session)
        {
            var sceneView = SceneView.currentDrawingSceneView;
            var camera = sceneView != null ? sceneView.camera : null;
            if (camera != null)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, _planes);
                session.QueryVisible(_planes, fanlight.transform.localToWorldMatrix, _visibleBlocks);
                return;
            }

            _visibleBlocks.Clear();

            for (var i = 0; i < layout.BlockCount; i++) _visibleBlocks.Add(i);
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
                    if (HandleUtility.nearestControl == controlId) HandleUtility.Repaint();
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
                    if (HandleUtility.nearestControl == controlId && GUI.enabled && GUIUtility.hotControl == 0 && !current.alt)
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
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
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

        internal static void DrawTransformHandle(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> selectedBlocks,
            int activeBlockIndex)
        {
            if (Application.isPlaying || selectedBlocks.Count == 0) return;

            var localCenter = Vector3.zero;
            for (var i = 0; i < selectedBlocks.Count; i++)
            {
                localCenter += layout.GetBlock(selectedBlocks[i]).Placement.position;
            }

            localCenter /= selectedBlocks.Count;
            var activePlacement = layout.GetBlock(activeBlockIndex).Placement;
            var worldCenter = fanlight.transform.TransformPoint(localCenter);
            var worldRotation = fanlight.transform.rotation * activePlacement.Rotation;
            var handleRotation = Tools.pivotRotation == PivotRotation.Local ? worldRotation : Quaternion.identity;

            EditorGUI.BeginChangeCheck();
            var nextCenterWorld = Handles.PositionHandle(worldCenter, handleRotation);
            var nextRotationWorld = Handles.RotationHandle(worldRotation, nextCenterWorld);
            if (!EditorGUI.EndChangeCheck()) return;

            var nextCenter = fanlight.transform.InverseTransformPoint(nextCenterWorld);
            var nextActiveRotation = Quaternion.Inverse(fanlight.transform.rotation) * nextRotationWorld;
            var rotationDelta = nextActiveRotation * Quaternion.Inverse(activePlacement.Rotation);
            var placements = new FanlightBlockPlacement[selectedBlocks.Count];

            for (var i = 0; i < selectedBlocks.Count; i++)
            {
                var placement = layout.GetBlock(selectedBlocks[i]).Placement;
                placements[i] = new FanlightBlockPlacement
                {
                    position = nextCenter + rotationDelta * (placement.position - localCenter),
                    eulerRotation = (rotationDelta * placement.Rotation).eulerAngles
                };
            }

            session.SetBlockPlacements(selectedBlocks, placements, "Edit Fanlight Block Placement");
        }

        internal static void DrawRowsAndHandles(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            int blockIndex)
        {
            var block = layout.GetBlock(blockIndex);
            var placement = block.Placement;
            var localToWorld = fanlight.transform.localToWorldMatrix
                               * Matrix4x4.TRS(placement.position, placement.Rotation, Vector3.one);
            var rowIndex = GetSelectedRowIndex(layout);

            for (var i = 0; i < block.RowCount; i++)
            {
                var row = block.GetRow(i);
                var points = new Vector3[17];
                for (var segment = 0; segment < points.Length; segment++)
                {
                    var t = (float)segment / (points.Length - 1);
                    var inverse = 1f - t;
                    var point = inverse * inverse * row.LeftPoint
                                + 2f * inverse * t * row.ControlPoint
                                + t * t * row.RightPoint;
                    points[segment] = localToWorld.MultiplyPoint3x4(point);
                }

                Handles.color = i == rowIndex ? SelectedColor : BlockColor;
                Handles.DrawAAPolyLine(i == rowIndex ? 3f : 1f, points);
            }

            DrawCageHandles(fanlight, layout, session, blockIndex, localToWorld);
            DrawRowHandles(fanlight, layout, session, blockIndex, rowIndex, localToWorld);
        }

        internal static void DrawRiseHandle(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> selectedBlocks,
            int activeBlockIndex)
        {
            if (Application.isPlaying || selectedBlocks.Count == 0) return;

            var transform = fanlight.transform;
            var worldUp = transform.TransformDirection(Vector3.up).normalized;
            var activeBlock = layout.GetBlock(activeBlockIndex);
            var backRow = activeBlock.GetRow(activeBlock.RowCount - 1);
            var backCenter = (backRow.LeftPoint + backRow.ControlPoint + backRow.RightPoint) / 3f;
            var backLayoutPoint = activeBlock.Placement.position + activeBlock.Placement.Rotation * backCenter;
            var backWorldPoint = transform.TransformPoint(backLayoutPoint);
            var riseSize = HandleUtility.GetHandleSize(backWorldPoint) * 0.65f;
            Handles.color = new Color(1f, 0.45f, 0.18f, 1f);
            EditorGUI.BeginChangeCheck();
            var nextBackWorldPoint = Handles.Slider(
                backWorldPoint,
                worldUp,
                riseSize,
                Handles.ArrowHandleCap,
                EditorSnapSettings.move.y);
            if (!EditorGUI.EndChangeCheck()) return;

            var nextBackLayoutPoint = transform.InverseTransformPoint(nextBackWorldPoint);
            var riseDelta = nextBackLayoutPoint.y - backLayoutPoint.y;
            FanlightLayoutHeightUtility.AddRise(layout, session, selectedBlocks, riseDelta);
        }

        private static void DrawCageHandles(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            int blockIndex,
            Matrix4x4 blockLocalToWorld)
        {
            var block = layout.GetBlock(blockIndex);
            if (block.RowCount < 2) return;

            var first = block.GetRow(0);
            var last = block.GetRow(block.RowCount - 1);
            var localCage = new[] { first.LeftPoint, first.RightPoint, last.RightPoint, last.LeftPoint };
            var worldCage = new Vector3[4];
            var changed = false;

            Handles.color = SelectedColor;
            for (var i = 0; i < worldCage.Length; i++)
            {
                var world = blockLocalToWorld.MultiplyPoint3x4(localCage[i]);
                var size = HandleUtility.GetHandleSize(world) * 0.06f;
                EditorGUI.BeginChangeCheck();
                worldCage[i] = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.RectangleHandleCap);
                changed |= EditorGUI.EndChangeCheck();
            }

            if (!changed) return;

            var worldToBlock = blockLocalToWorld.inverse;
            for (var i = 0; i < localCage.Length; i++) localCage[i] = worldToBlock.MultiplyPoint3x4(worldCage[i]);

            var rows = block.CopyRows();
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var source = rows[rowIndex];
                var t = (float)rowIndex / (rows.Length - 1);
                var left = Vector3.Lerp(localCage[0], localCage[3], t);
                var right = Vector3.Lerp(localCage[1], localCage[2], t);
                var oldMidpoint = (source.LeftPoint + source.RightPoint) * 0.5f;
                var newMidpoint = (left + right) * 0.5f;
                rows[rowIndex] = new FanlightLayoutRow(
                    left,
                    source.ControlPoint + newMidpoint - oldMidpoint,
                    right,
                    source.CopyStableSeatIds());
            }

            session.SetBlockRows(blockIndex, rows, "Edit Fanlight Block Cage");
        }

        private static void DrawRowHandles(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            int blockIndex,
            int rowIndex,
            Matrix4x4 blockLocalToWorld)
        {
            if (rowIndex < 0) return;

            var row = layout.GetBlock(blockIndex).GetRow(rowIndex);
            var rotation = fanlight.transform.rotation * layout.GetBlock(blockIndex).Placement.Rotation;
            var leftWorld = blockLocalToWorld.MultiplyPoint3x4(row.LeftPoint);
            var controlWorld = blockLocalToWorld.MultiplyPoint3x4(row.ControlPoint);
            var rightWorld = blockLocalToWorld.MultiplyPoint3x4(row.RightPoint);

            EditorGUI.BeginChangeCheck();
            leftWorld = Handles.PositionHandle(leftWorld, rotation);
            controlWorld = Handles.PositionHandle(controlWorld, rotation);
            rightWorld = Handles.PositionHandle(rightWorld, rotation);
            if (!EditorGUI.EndChangeCheck()) return;

            var worldToBlock = blockLocalToWorld.inverse;
            session.SetRowGeometry(
                blockIndex,
                rowIndex,
                worldToBlock.MultiplyPoint3x4(leftWorld),
                worldToBlock.MultiplyPoint3x4(controlWorld),
                worldToBlock.MultiplyPoint3x4(rightWorld),
                "Edit Fanlight Row Geometry");
        }

        private static void DrawSelectedSeatDots(Transform transform, FanlightLayoutEditSession session, int blockIndex)
        {
            var block = session.RuntimeLayout.Blocks[blockIndex];
            var end = block.startIndex + block.count;
            var step = Mathf.Max(1, Mathf.CeilToInt((float)block.count / MaximumSeatDots));
            var color = SelectedColor;
            color.a = 0.7f;
            Handles.color = color;

            for (var i = block.startIndex; i < end; i += step)
            {
                var packed = session.RuntimeLayout.Seats[i].localPositionSeed;
                var world = transform.TransformPoint(new Vector3(packed.x, packed.y, packed.z));
                var size = HandleUtility.GetHandleSize(world) * 0.025f;
                Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
            }
        }
    }
}
