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
        private const float HeightHandleOffset = 0.45f;
        private const int MaximumSeatDots = 512;

        internal static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        internal static readonly Color SelectedColor = new(1.0f, 0.82f, 0.2f, 0.95f);
        private static readonly Color HeightColor = new(1f, 0.45f, 0.18f, 1f);
        private readonly List<int> _visibleBlocks = new();
        private readonly List<int> _selectedBlocks = new();
        private readonly Plane[] _planes = new Plane[6];
        private bool _rotationDragging;
        private PrismFanlight _rotationFanlight;
        private FanlightLayoutAsset _rotationLayout;
        private int[] _rotationBlockIndices = Array.Empty<int>();
        private FanlightBlockPlacement[] _rotationPlacements = Array.Empty<FanlightBlockPlacement>();
        private Vector3 _rotationLocalCenter;
        private Quaternion _rotationBaseHandleRotation = Quaternion.identity;
        private Quaternion _rotationHandleRotation = Quaternion.identity;


        // Methods

        internal static int GetSelectedBlockIndex(FanlightLayoutAsset layout)
            => FanlightLayoutSelection.GetActiveIndex(layout);

        internal static void GetSelectedBlockIndices(FanlightLayoutAsset layout, List<int> results)
        {
            FanlightLayoutSelection.GetIndices(layout, results);
        }

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

        internal static void DrawMoveHandle(
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
                localCenter += session.GetBlockBounds(selectedBlocks[i]).center;
            }

            localCenter /= selectedBlocks.Count;
            var activePlacement = layout.GetBlock(activeBlockIndex).Placement;
            var worldCenter = fanlight.transform.TransformPoint(localCenter);
            var worldRotation = fanlight.transform.rotation * activePlacement.Rotation;
            var handleRotation = Tools.pivotRotation == PivotRotation.Local ? worldRotation : Quaternion.identity;

            EditorGUI.BeginChangeCheck();
            var nextCenterWorld = Handles.PositionHandle(worldCenter, handleRotation);
            if (!EditorGUI.EndChangeCheck()) return;

            var nextCenter = fanlight.transform.InverseTransformPoint(nextCenterWorld);
            var delta = nextCenter - localCenter;
            var placements = new FanlightBlockPlacement[selectedBlocks.Count];

            for (var i = 0; i < selectedBlocks.Count; i++)
            {
                var placement = layout.GetBlock(selectedBlocks[i]).Placement;
                placement.position += delta;
                placements[i] = placement;
            }

            session.SetBlockPlacements(selectedBlocks, placements, "Move Fanlight Blocks");
        }

        internal void DrawRotateHandle(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> selectedBlocks)
        {
            if (Application.isPlaying || selectedBlocks.Count == 0)
            {
                ResetRotateHandle();
                return;
            }

            if (_rotationDragging && !IsRotationDragValid(fanlight, layout, selectedBlocks))
            {
                ResetRotateHandle();
            }

            var localCenter = _rotationDragging ? _rotationLocalCenter : Vector3.zero;
            if (!_rotationDragging)
            {
                for (var i = 0; i < selectedBlocks.Count; i++)
                {
                    localCenter += session.GetBlockBounds(selectedBlocks[i]).center;
                }

                localCenter /= selectedBlocks.Count;
            }

            var worldCenter = fanlight.transform.TransformPoint(localCenter);
            var handleRotation = _rotationDragging
                ? _rotationHandleRotation
                : fanlight.transform.rotation;

            var hotControlBefore = GUIUtility.hotControl;
            EditorGUI.BeginChangeCheck();
            var nextHandleRotation = Handles.RotationHandle(handleRotation, worldCenter);
            var changed = EditorGUI.EndChangeCheck();
            var hotControlAfter = GUIUtility.hotControl;

            if (!_rotationDragging && hotControlBefore == 0 && hotControlAfter != 0)
            {
                BeginRotationDrag(fanlight, layout, selectedBlocks, localCenter);
            }

            if (!_rotationDragging) return;

            _rotationHandleRotation = nextHandleRotation;
            if (changed)
            {
                var worldRotationDelta = _rotationHandleRotation
                                         * Quaternion.Inverse(_rotationBaseHandleRotation);
                var rotationDelta = Quaternion.Inverse(_rotationBaseHandleRotation)
                                    * worldRotationDelta
                                    * _rotationBaseHandleRotation;
                var placements = new FanlightBlockPlacement[_rotationPlacements.Length];

                for (var i = 0; i < placements.Length; i++)
                {
                    var placement = _rotationPlacements[i];
                    placements[i] = new FanlightBlockPlacement
                    {
                        position = _rotationLocalCenter
                                   + rotationDelta * (placement.position - _rotationLocalCenter),
                        eulerRotation = (rotationDelta * placement.Rotation).eulerAngles
                    };
                }

                session.SetBlockPlacements(_rotationBlockIndices, placements, "Rotate Fanlight Blocks");
            }

            if (hotControlAfter == 0) ResetRotateHandle();
        }

        internal void ResetRotateHandle()
        {
            _rotationDragging = false;
            _rotationFanlight = null;
            _rotationLayout = null;
            _rotationBlockIndices = Array.Empty<int>();
            _rotationPlacements = Array.Empty<FanlightBlockPlacement>();
            _rotationLocalCenter = Vector3.zero;
            _rotationBaseHandleRotation = Quaternion.identity;
            _rotationHandleRotation = Quaternion.identity;
        }

        private void BeginRotationDrag(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> selectedBlocks,
            Vector3 localCenter)
        {
            _rotationDragging = true;
            _rotationFanlight = fanlight;
            _rotationLayout = layout;
            _rotationBlockIndices = new int[selectedBlocks.Count];
            _rotationPlacements = new FanlightBlockPlacement[selectedBlocks.Count];
            _rotationLocalCenter = localCenter;
            _rotationBaseHandleRotation = fanlight.transform.rotation;
            _rotationHandleRotation = _rotationBaseHandleRotation;

            for (var i = 0; i < _rotationBlockIndices.Length; i++)
            {
                var blockIndex = selectedBlocks[i];
                _rotationBlockIndices[i] = blockIndex;
                _rotationPlacements[i] = layout.GetBlock(blockIndex).Placement;
            }
        }

        private bool IsRotationDragValid(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> selectedBlocks)
        {
            if (_rotationFanlight != fanlight
                || _rotationLayout != layout
                || _rotationBlockIndices.Length != selectedBlocks.Count)
            {
                return false;
            }

            for (var i = 0; i < _rotationBlockIndices.Length; i++)
            {
                if (_rotationBlockIndices[i] != selectedBlocks[i]) return false;
            }

            return true;
        }

        internal static bool DrawShapeHandles(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            int blockIndex)
        {
            if (Application.isPlaying) return false;

            var transform = fanlight.transform;
            var block = layout.GetBlock(blockIndex);
            var placement = block.Placement;
            var worldRotation = transform.rotation * placement.Rotation;
            var worldUp = worldRotation * Vector3.up;
            var worldRight = worldRotation * Vector3.right;
            var worldForward = worldRotation * Vector3.forward;
            var snap = EditorSnapSettings.snapEnabled
                ? new Vector2(EditorSnapSettings.move.x, EditorSnapSettings.move.z)
                : Vector2.zero;
            var changed = false;

            for (var handle = 0; handle < FanlightLayoutShapeUtility.GetHandleCount(block.RowCount); handle++)
            {
                var blockPoint = FanlightLayoutShapeUtility.GetHandleBlockPoint(handle, block);
                var layoutPoint = placement.position + placement.Rotation * blockPoint;
                var worldPoint = transform.TransformPoint(layoutPoint);
                var size = HandleUtility.GetHandleSize(worldPoint) * 0.08f;
                Handles.color = handle >= 4 || block.RowCount == 1 && handle == 2
                    ? HeightColor
                    : SelectedColor;
                EditorGUI.BeginChangeCheck();
                var nextWorldPoint = Handles.Slider2D(
                    worldPoint,
                    worldUp,
                    worldRight,
                    worldForward,
                    size,
                    Handles.RectangleHandleCap,
                    snap,
                    true);
                if (!EditorGUI.EndChangeCheck()) continue;

                var nextLayoutPoint = transform.InverseTransformPoint(nextWorldPoint);
                var nextBlockPoint = Quaternion.Inverse(placement.Rotation)
                                     * (nextLayoutPoint - placement.position);
                var rows = FanlightLayoutShapeUtility.CreateRows(block.CopyRows(), handle, nextBlockPoint);
                changed |= session.SetBlockRows(blockIndex, rows, "Shape Fanlight Block");
                block = layout.GetBlock(blockIndex);
            }

            return changed;
        }

        internal static bool DrawHeightHandles(
            PrismFanlight fanlight,
            FanlightLayoutAsset layout,
            FanlightLayoutEditSession session,
            IReadOnlyList<int> selectedBlocks,
            int activeBlockIndex)
        {
            if (Application.isPlaying || selectedBlocks.Count == 0) return false;

            var transform = fanlight.transform;
            var worldUp = transform.TransformDirection(Vector3.up).normalized;
            var activeBlock = layout.GetBlock(activeBlockIndex);
            if (activeBlock.RowCount < 2) return false;

            var placement = activeBlock.Placement;
            var changed = false;
            for (var edgeIndex = 0; edgeIndex < 4; edgeIndex++)
            {
                var edge = (FanlightLayoutHeightUtility.Edge)edgeIndex;
                var edgeBlockPoint = FanlightLayoutHeightUtility.GetEdgeBlockPoint(activeBlock, edge);
                var edgeLayoutPoint = placement.position + placement.Rotation * edgeBlockPoint;
                var edgeWorldPoint = transform.TransformPoint(edgeLayoutPoint);
                var outwardBlock = FanlightLayoutHeightUtility.GetEdgeOutwardBlockDirection(activeBlock, edge);
                var outwardLayout = placement.Rotation * outwardBlock;
                var outwardWorld = transform.TransformDirection(outwardLayout).normalized;
                var handleOffset = HandleUtility.GetHandleSize(edgeWorldPoint) * HeightHandleOffset;
                var handleWorldPoint = edgeWorldPoint + outwardWorld * handleOffset;
                var handleLayoutPoint = transform.InverseTransformPoint(handleWorldPoint);
                var size = HandleUtility.GetHandleSize(handleWorldPoint) * 0.6f;

                Handles.color = HeightColor;
                Handles.DrawLine(edgeWorldPoint, handleWorldPoint);
                EditorGUI.BeginChangeCheck();
                var nextHandleWorldPoint = Handles.Slider(
                    handleWorldPoint,
                    worldUp,
                    size,
                    Handles.ArrowHandleCap,
                    EditorSnapSettings.move.y);
                if (!EditorGUI.EndChangeCheck()) continue;

                var nextHandleLayoutPoint = transform.InverseTransformPoint(nextHandleWorldPoint);
                var heightDelta = nextHandleLayoutPoint.y - handleLayoutPoint.y;
                changed |= FanlightLayoutHeightUtility.AddEdgeHeight(
                    layout,
                    session,
                    selectedBlocks,
                    edge,
                    heightDelta);
                activeBlock = layout.GetBlock(activeBlockIndex);
            }

            return changed;
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
