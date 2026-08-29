using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightLayoutEditorWindow : EditorWindow, IHasCustomMenu
    {
        private enum LayoutTool
        {
            Move,
            Rotate,
            Shape
        }

        private enum CanvasUpDirection
        {
            PositiveZ,
            PositiveX,
            NegativeZ,
            NegativeX
        }


        private sealed class BendPopupContent : PopupWindowContent
        {
            // Fields

            private readonly Action<float> _apply;
            private float _bendDegrees;


            // Methods

            internal BendPopupContent(float bendDegrees, Action<float> apply)
            {
                _bendDegrees = bendDegrees;
                _apply = apply;
            }

            public override Vector2 GetWindowSize() => new(230f, 86f);

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.LabelField("Bend Selected Blocks", EditorStyles.boldLabel);
                _bendDegrees = EditorGUILayout.FloatField("Angle", _bendDegrees);
                using (new EditorGUI.DisabledScope(!float.IsFinite(_bendDegrees)))
                {
                    if (!GUILayout.Button("Apply")) return;

                    _apply?.Invoke(_bendDegrees);
                    editorWindow.Close();
                }
            }
        }

        private sealed class QuickGridPopupContent : PopupWindowContent
        {
            // Fields

            private readonly FanlightLayoutEditorWindow _owner;
            private readonly FanlightLayoutAsset _layout;
            private Vector2Int _blockCount;
            private Vector2Int _seatsPerBlock;
            private Vector2 _seatSpacing;
            private Vector2 _aisleWidth;


            // Methods

            internal QuickGridPopupContent(FanlightLayoutEditorWindow owner)
            {
                _owner = owner;
                _layout = owner._layout;
                _blockCount = owner._quickBlockCount;
                _seatsPerBlock = owner._quickSeatsPerBlock;
                _seatSpacing = owner._quickSeatSpacing;
                _aisleWidth = owner._quickAisleWidth;
            }

            public override Vector2 GetWindowSize() => new(430f, 310f);

            public override void OnGUI(Rect rect)
            {
                _blockCount = Vector2Int.Max(
                    EditorGUILayout.Vector2IntField("Block Count", _blockCount),
                    Vector2Int.one);
                _seatsPerBlock = Vector2Int.Max(
                    EditorGUILayout.Vector2IntField("Seats Per Block", _seatsPerBlock),
                    Vector2Int.one);
                _seatSpacing = Vector2.Max(
                    EditorGUILayout.Vector2Field("Seat Spacing", _seatSpacing),
                    Vector2.one * 0.001f);
                _aisleWidth = Vector2.Max(
                    EditorGUILayout.Vector2Field("Aisle Width", _aisleWidth),
                    Vector2.zero);

                var valid = TryGetQuickGridCounts(
                    _blockCount,
                    _seatsPerBlock,
                    out var totalBlocks,
                    out var totalSeats);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Total Blocks", valid ? totalBlocks.ToString("N0") : "Unsupported");
                EditorGUILayout.LabelField("Total Seats", valid ? totalSeats.ToString("N0") : "Unsupported");

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (!GUILayout.Button("Replace Layout & Bake", GUILayout.Height(30f))) return;
                }

                if (_owner == null || _layout == null || _owner._layout != _layout)
                {
                    EditorUtility.DisplayDialog(
                        "Quick Grid Target Changed",
                        "The Layout Editor target changed. Open the generator again for the current Layout.",
                        "OK");
                    editorWindow.Close();
                    return;
                }

                var confirmed = EditorUtility.DisplayDialog(
                    "Replace Fanlight Layout?",
                    $"Current: {_layout.BlockCount:N0} Blocks / {_layout.TotalSeatCount:N0} Seats\n"
                    + $"New: {totalBlocks:N0} Blocks / {totalSeats:N0} Seats\n\n"
                    + "All current Blocks, Rows, Stable IDs, and the Bake will be replaced. Loaded Baseline Block Palette mappings reset to Slot 0; other saved mappings must be recreated. This can be undone once with Undo.",
                    "Replace Layout & Bake",
                    "Cancel");
                if (!confirmed) return;

                _owner.SetQuickGridSettings(_blockCount, _seatsPerBlock, _seatSpacing, _aisleWidth);

                if (_owner.CreateQuickGrid("Regenerate Fanlight Quick Grid"))
                {
                    editorWindow.Close();
                }
            }
        }


        // Fields

        private const float MinimumZoom = 8f;
        private const float MaximumZoom = 240f;
        private const float ShapeHandleRadius = 8f;
        private const string WindowTitle = "Fanlight Layout";


        [SerializeField]
        private PrismFanlight _target;

        [SerializeField]
        private bool _locked;

        [SerializeField]
        private CanvasUpDirection _canvasUpDirection;


        private FanlightLayoutAsset _layout;
        private LayoutTool _tool = LayoutTool.Move;
        private Vector2 _pan;
        private float _zoom = 42f;
        private float _positionSnap = 0.1f;
        private float _angleSnap = 5f;
        private bool _snapPosition = true;
        private bool _snapAngle = true;
        private bool _panning;
        private bool _marquee;
        private bool _marqueeAdditive;
        private bool _transforming;
        private int _shapeHandle = -1;
        private int _undoGroup = -1;
        private int _pressedBlock = -1;
        private bool _dragChanged;
        private Vector2 _dragStartMouse;
        private Vector2 _marqueeCurrent;
        private Vector3 _transformPivot;
        private float _transformStartAngle;
        private int[] _transformIndices = Array.Empty<int>();
        private FanlightBlockPlacement[] _transformPlacements = Array.Empty<FanlightBlockPlacement>();
        private FanlightLayoutRow[] _shapeRows = Array.Empty<FanlightLayoutRow>();
        private Vector2Int _quickBlockCount = new(7, 3);
        private Vector2Int _quickSeatsPerBlock = new(8, 12);
        private Vector2 _quickSeatSpacing = new(0.4f, 0.8f);
        private Vector2 _quickAisleWidth = new(0.7f, 1.2f);
        private FanlightLayoutGenerator.SeatAnchor _seatAnchor = FanlightLayoutGenerator.SeatAnchor.Center;

        private static FanlightLayoutEditorWindow _activeWindow;
        private static GUIContent _saveIcon;

        private readonly List<int> _selectedBlocks = new();


        // Properties

        internal static PrismFanlight ActiveTarget => _activeWindow != null ? _activeWindow._target : null;

        private static GUIContent SaveIcon => _saveIcon ??= new GUIContent(EditorGUIUtility.IconContent("SaveActive").image, "Bake");


        // Methods

        [MenuItem("Window/Prism Fanlight/Layout Editor")]
        private static void OpenFromMenu()
        {
            var window = GetWindow<FanlightLayoutEditorWindow>();
            window.titleContent = new GUIContent("Fanlight Layout");
            window.UseCurrentSelection();
            window.Show();
            window.ActivateLayoutTool();
        }

        internal static void Open(PrismFanlight target)
        {
            var window = GetWindow<FanlightLayoutEditorWindow>();
            window.titleContent = new GUIContent("Fanlight Layout");
            window.SetTarget(target);
            window.Show();
            window.Focus();
            window.ActivateLayoutTool();
        }

        private void OnEnable()
        {
            _activeWindow = this;
            minSize = new Vector2(640f, 420f);

            FanlightLayoutSelection.Changed += OnLayoutSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedo;

            if (_target == null)
            {
                UseCurrentSelection();
            }
            else
            {
                SetTarget(_target);
            }
        }

        private void OnDisable()
        {
            if (_activeWindow == this) _activeWindow = null;
            FanlightLayoutSelection.Changed -= OnLayoutSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            ToolManager.RefreshAvailableTools();
            SceneView.RepaintAll();
        }

        private void OnFocus()
        {
            _activeWindow = this;
            UpdateTitle();
            ToolManager.RefreshAvailableTools();
            SceneView.RepaintAll();
        }

        private void OnInspectorUpdate()
        {
            UpdateTitle();
        }

        private void OnSelectionChange()
        {
            if (!_locked) UseCurrentSelection();
            Repaint();
        }

        private void OnLayoutSelectionChanged()
        {
            Repaint();
            ActivateLayoutTool();
        }

        private void OnUndoRedo()
        {
            if (_layout != null)
            {
                FanlightLayoutEditSession.Reset(_layout);
                FanlightLayoutIdRegistry.Invalidate();
                if (_layout.IsInitialized)
                {
                    FanlightLayoutEditSession.Get(_layout)?.ApplyPreviewToAllInstances(-1);
                }
            }

            UpdateTitle();
            Repaint();
        }

        private void OnGUI()
        {
            RefreshTargetLayout();
            UpdateTitle();

            if (_target == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject with a Prism Fanlight component.", MessageType.Info);
                return;
            }

            if (_layout == null)
            {
                EditorGUILayout.HelpBox("The selected Prism Fanlight has no Layout Asset.", MessageType.Info);
                return;
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Layout authoring is disabled in Play Mode.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (!_layout.IsInitialized)
                {
                    DrawQuickGrid();
                    return;
                }

                if (FanlightLayoutIdRegistry.IsDuplicate(_layout))
                {
                    EditorGUILayout.HelpBox("Duplicate Layout ID detected. Editing and baking are disabled.", MessageType.Error);
                    return;
                }

                DrawToolbar();
                DrawCanvas();

                if (FanlightLayoutSelection.IsAdvancedRowEditing(_layout)) DrawAdvancedRows();
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Lock"), _locked, ToggleLock);
            menu.AddSeparator(string.Empty);
        }

        private void DrawQuickGrid()
        {
            GUILayout.Space(18f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MaxWidth(520f)))
            {
                EditorGUILayout.LabelField("Quick Grid", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Create a ready-to-edit flat layout from the familiar grid inputs.", EditorStyles.wordWrappedLabel);
                GUILayout.Space(8f);

                _quickBlockCount = Vector2Int.Max(
                    EditorGUILayout.Vector2IntField("Block Count", _quickBlockCount),
                    Vector2Int.one);
                _quickSeatsPerBlock = Vector2Int.Max(
                    EditorGUILayout.Vector2IntField("Seats Per Block", _quickSeatsPerBlock),
                    Vector2Int.one);
                _quickSeatSpacing = Vector2.Max(
                    EditorGUILayout.Vector2Field("Seat Spacing", _quickSeatSpacing),
                    Vector2.one * 0.001f);
                _quickAisleWidth = Vector2.Max(
                    EditorGUILayout.Vector2Field("Aisle Width", _quickAisleWidth),
                    Vector2.zero);

                var valid = TryGetQuickGridCounts(out var totalBlocks, out var totalSeats);
                EditorGUILayout.LabelField("Total Blocks", valid ? totalBlocks.ToString("N0") : "Unsupported");
                EditorGUILayout.LabelField("Total Seats", valid ? totalSeats.ToString("N0") : "Unsupported");

                GUILayout.Space(8f);
                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (GUILayout.Button("Create & Bake", GUILayout.Height(30f)))
                    {
                        CreateQuickGrid("Create Fanlight Quick Grid");
                    }
                }
            }
        }

        private void DrawToolbar()
        {
            FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawToolToggle(LayoutTool.Move, EditorGUIUtility.TrIconContent("MoveTool", "Move Tool"));
                DrawToolToggle(LayoutTool.Rotate, EditorGUIUtility.TrIconContent("RotateTool", "Rotate Tool"));
                DrawToolToggle(LayoutTool.Shape, EditorGUIUtility.TrIconContent("RectTool", "Shape Tool"));

                GUILayout.Space(16f);

                if (_tool == LayoutTool.Move)
                {
                    _snapPosition = GUILayout.Toggle(_snapPosition, "Snap", EditorStyles.toolbarButton, GUILayout.Width(46f));
                    _positionSnap = Mathf.Max(0.001f, EditorGUILayout.FloatField(_positionSnap, GUILayout.Width(52f)));
                }
                else if (_tool == LayoutTool.Rotate)
                {
                    _snapAngle = GUILayout.Toggle(_snapAngle, "Snap", EditorStyles.toolbarButton, GUILayout.Width(46f));
                    _angleSnap = Mathf.Max(0.1f, EditorGUILayout.FloatField(_angleSnap, GUILayout.Width(52f)));
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        new GUIContent($"Up {GetCanvasUpLabel()}", "Rotate the canvas up direction by 90 degrees."),
                        EditorStyles.toolbarButton,
                        GUILayout.Width(58f)))
                {
                    RotateCanvasUp();
                }

                if (GUILayout.Button(SaveIcon, EditorStyles.toolbarButton))
                {
                    FanlightLayoutEditSession.Get(_layout)?.Bake();
                }
            }
        }

        private void DrawToolToggle(LayoutTool tool, GUIContent label)
        {
            var selected = _tool == tool;

            if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton) && !selected)
            {
                _tool = tool;
                Repaint();
            }
        }

        private void RotateCanvasUp()
        {
            var centerView = new Vector2(-_pan.x / _zoom, _pan.y / _zoom);
            var centerLocal = ViewToLocal(centerView);
            _canvasUpDirection = (CanvasUpDirection)(((int)_canvasUpDirection + 1) % 4);
            var nextCenterView = LocalToView(centerLocal);
            _pan = new Vector2(-nextCenterView.x * _zoom, nextCenterView.y * _zoom);
            Repaint();
        }

        private string GetCanvasUpLabel()
            => _canvasUpDirection switch
            {
                CanvasUpDirection.PositiveX => "+X",
                CanvasUpDirection.NegativeZ => "-Z",
                CanvasUpDirection.NegativeX => "-X",
                _ => "+Z"
            };

        private void DrawCanvas()
        {
            var viewport = GUILayoutUtility.GetRect(100f, 100000f, 180f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(viewport, new Color(0.105f, 0.11f, 0.12f, 1f));
            var session = FanlightLayoutEditSession.Get(_layout);
            if (session == null) return;

            GUI.BeginClip(viewport);
            try
            {
                var canvas = new Rect(Vector2.zero, viewport.size);
                FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);
                HandleCanvasNavigation(canvas);
                DrawGrid(canvas, _layout.ReferenceSeatSpacing.x, _layout.ReferenceSeatSpacing.y);
                DrawBlocks(canvas, session);
                if (_tool == LayoutTool.Shape && _selectedBlocks.Count == 1) DrawShapeHandles(canvas, session);
                if (_marquee) DrawMarquee();
                HandleCanvasInput(canvas, session);
            }
            finally
            {
                GUI.EndClip();
            }
        }

        private void DrawGrid(Rect canvas, float horizontalStep, float verticalStep)
        {
            if (_canvasUpDirection is CanvasUpDirection.PositiveX or CanvasUpDirection.NegativeX)
            {
                (horizontalStep, verticalStep) = (verticalStep, horizontalStep);
            }

            horizontalStep = Mathf.Max(0.001f, horizontalStep);
            verticalStep = Mathf.Max(0.001f, verticalStep);
            var horizontalPixels = horizontalStep * _zoom;
            while (horizontalPixels < 24f)
            {
                horizontalStep *= 2f;
                horizontalPixels = horizontalStep * _zoom;
            }

            var verticalPixels = verticalStep * _zoom;
            while (verticalPixels < 24f)
            {
                verticalStep *= 2f;
                verticalPixels = verticalStep * _zoom;
            }

            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            var origin = ViewToCanvas(Vector2.zero, canvas);
            for (var x = origin.x % horizontalPixels; x < canvas.xMax; x += horizontalPixels)
            {
                if (x >= canvas.xMin) Handles.DrawLine(new Vector3(x, canvas.yMin), new Vector3(x, canvas.yMax));
            }

            for (var y = origin.y % verticalPixels; y < canvas.yMax; y += verticalPixels)
            {
                if (y >= canvas.yMin) Handles.DrawLine(new Vector3(canvas.xMin, y), new Vector3(canvas.xMax, y));
            }

            Handles.color = new Color(1f, 1f, 1f, 0.28f);
            Handles.DrawLine(new Vector3(canvas.xMin, origin.y), new Vector3(canvas.xMax, origin.y));
            Handles.DrawLine(new Vector3(origin.x, canvas.yMin), new Vector3(origin.x, canvas.yMax));
            Handles.EndGUI();
        }

        private void DrawBlocks(Rect canvas, FanlightLayoutEditSession session)
        {
            Handles.BeginGUI();
            for (var blockIndex = 0; blockIndex < _layout.BlockCount; blockIndex++)
            {
                var corners = session.GetCorners(blockIndex);
                var points = new Vector3[]
                {
                    LocalToCanvas(corners[0], canvas),
                    LocalToCanvas(corners[1], canvas),
                    LocalToCanvas(corners[2], canvas),
                    LocalToCanvas(corners[3], canvas)
                };
                var selected = _selectedBlocks.Contains(blockIndex);
                Handles.color = selected
                    ? new Color(1f, 0.82f, 0.2f, 0.18f)
                    : new Color(0.1f, 0.85f, 1f, 0.08f);
                Handles.DrawAAConvexPolygon(points);
                Handles.color = selected ? FanlightLayoutScenePreview.SelectedColor : FanlightLayoutScenePreview.BlockColor;
                Handles.DrawAAPolyLine(selected ? 3f : 1.5f, points[0], points[1], points[2], points[3], points[0]);
            }

            Handles.EndGUI();
        }

        private void DrawShapeHandles(Rect canvas, FanlightLayoutEditSession session)
        {
            var active = _selectedBlocks[0];
            var points = GetShapeHandlePoints(active, session);
            for (var i = 0; i < points.Length; i++)
            {
                var center = LocalToCanvas(points[i], canvas);
                var rect = new Rect(
                    center.x - ShapeHandleRadius,
                    center.y - ShapeHandleRadius,
                    ShapeHandleRadius * 2f,
                    ShapeHandleRadius * 2f);
                EditorGUI.DrawRect(rect, i >= 4
                    ? new Color(1f, 0.45f, 0.18f, 1f)
                    : FanlightLayoutScenePreview.SelectedColor);
            }
        }

        private void DrawMarquee()
        {
            var rect = Rect.MinMaxRect(
                Mathf.Min(_dragStartMouse.x, _marqueeCurrent.x),
                Mathf.Min(_dragStartMouse.y, _marqueeCurrent.y),
                Mathf.Max(_dragStartMouse.x, _marqueeCurrent.x),
                Mathf.Max(_dragStartMouse.y, _marqueeCurrent.y));
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.65f, 1f, 0.12f));
            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.75f, 1f, 0.9f);
            Handles.DrawAAPolyLine(2f,
                new Vector3(rect.xMin, rect.yMin),
                new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMax),
                new Vector3(rect.xMin, rect.yMax),
                new Vector3(rect.xMin, rect.yMin));
            Handles.EndGUI();
        }

        private void HandleCanvasNavigation(Rect canvas)
        {
            var current = Event.current;
            if (current.type == EventType.MouseLeaveWindow)
            {
                _panning = false;
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0 && _panning)
            {
                _panning = false;
                current.Use();
                return;
            }

            if (!canvas.Contains(current.mousePosition) && !_panning) return;

            if (current.type == EventType.ScrollWheel)
            {
                var before = CanvasToView(current.mousePosition, canvas);
                _zoom = Mathf.Clamp(_zoom * Mathf.Pow(1.1f, -current.delta.y), MinimumZoom, MaximumZoom);
                var after = CanvasToView(current.mousePosition, canvas);
                _pan += new Vector2((after.x - before.x) * _zoom, -(after.y - before.y) * _zoom);
                current.Use();
                Repaint();
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && current.alt)
            {
                _panning = true;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0 && _panning)
            {
                _pan += current.delta;
                current.Use();
                Repaint();
            }
        }

        private void HandleCanvasInput(Rect canvas, FanlightLayoutEditSession session)
        {
            var current = Event.current;
            var insideCanvas = canvas.Contains(current.mousePosition);
            if (current.type == EventType.KeyDown && insideCanvas && !EditorGUIUtility.editingTextField)
            {
                if (EditorGUI.actionKey && current.keyCode == KeyCode.A)
                {
                    FanlightLayoutSelection.SelectAll(_layout);
                    current.Use();
                }
                else if (EditorGUI.actionKey && current.keyCode == KeyCode.C)
                {
                    FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);
                    FanlightLayoutClipboard.Copy(_layout, _selectedBlocks);
                    current.Use();
                }
                else if (EditorGUI.actionKey && current.keyCode == KeyCode.V)
                {
                    FanlightLayoutClipboard.Paste(_layout, CanvasToLocal(current.mousePosition, canvas));
                    current.Use();
                }
                else if (EditorGUI.actionKey && current.keyCode == KeyCode.D)
                {
                    DuplicateSelected();
                    current.Use();
                }
                else if (current.keyCode == KeyCode.Escape)
                {
                    FanlightLayoutSelection.Clear(_layout);
                    current.Use();
                }
                else if (current.keyCode is KeyCode.Delete or KeyCode.Backspace)
                {
                    DeleteSelected();
                    current.Use();
                }
            }

            if (!insideCanvas && !_marquee && !_transforming && _shapeHandle < 0) return;

            if (current.type == EventType.MouseDown && current.button == 1)
            {
                ShowCanvasMenu(CanvasToLocal(current.mousePosition, canvas));
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                _pressedBlock = -1;
                _dragChanged = false;
                FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);
                if (_tool == LayoutTool.Shape && _selectedBlocks.Count == 1)
                {
                    var handle = FindShapeHandle(current.mousePosition, canvas, session, _selectedBlocks[0]);
                    if (handle >= 0)
                    {
                        BeginShape(handle);
                        current.Use();
                        return;
                    }
                }

                var hit = FindBlock(current.mousePosition, canvas, session);
                if (hit >= 0)
                {
                    if (EditorGUI.actionKey)
                    {
                        FanlightLayoutSelection.Toggle(_layout, hit, true);
                        current.Use();
                        return;
                    }

                    if (!_selectedBlocks.Contains(hit))
                    {
                        FanlightLayoutSelection.SetOnly(_layout, hit);
                    }

                    _pressedBlock = hit;
                    FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);
                    if (_tool is LayoutTool.Move or LayoutTool.Rotate)
                    {
                        BeginTransform(current.mousePosition, session, canvas);
                    }

                    current.Use();
                    return;
                }

                _marquee = true;
                _marqueeAdditive = EditorGUI.actionKey;
                _dragStartMouse = current.mousePosition;
                _marqueeCurrent = current.mousePosition;
                if (!_marqueeAdditive) FanlightLayoutSelection.Clear(_layout);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (_transforming)
                {
                    _dragChanged = true;
                    UpdateTransform(current.mousePosition, canvas, session);
                    current.Use();
                }
                else if (_shapeHandle >= 0)
                {
                    UpdateShape(current.mousePosition, canvas, session);
                    current.Use();
                }
                else if (_marquee)
                {
                    _marqueeCurrent = current.mousePosition;
                    current.Use();
                    Repaint();
                }
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                if (_marquee) CompleteMarquee(canvas, session, _marqueeAdditive);
                if (!_marquee && !_dragChanged && _pressedBlock >= 0)
                {
                    FanlightLayoutSelection.SetOnly(_layout, _pressedBlock);
                }

                EndDrag();
                current.Use();
            }
        }

        private void BeginTransform(Vector2 mousePosition, FanlightLayoutEditSession session, Rect canvas)
        {
            if (_selectedBlocks.Count == 0) return;

            _transforming = true;
            _dragStartMouse = mousePosition;
            _transformIndices = _selectedBlocks.ToArray();
            _transformPlacements = new FanlightBlockPlacement[_transformIndices.Length];
            _transformPivot = Vector3.zero;
            for (var i = 0; i < _transformIndices.Length; i++)
            {
                _transformPlacements[i] = _layout.GetBlock(_transformIndices[i]).Placement;
                _transformPivot += session.GetBlockBounds(_transformIndices[i]).center;
            }

            _transformPivot /= _transformIndices.Length;
            if (_tool == LayoutTool.Rotate)
            {
                var start = CanvasToLocal(mousePosition, canvas);
                _transformStartAngle = Mathf.Atan2(start.z - _transformPivot.z, start.x - _transformPivot.x) * Mathf.Rad2Deg;
            }

            BeginUndo(_tool == LayoutTool.Move ? "Move Fanlight Blocks" : "Rotate Fanlight Blocks");
        }

        private void UpdateTransform(Vector2 mousePosition, Rect canvas, FanlightLayoutEditSession session)
        {
            if (_transformIndices.Length == 0) return;

            var placements = new FanlightBlockPlacement[_transformPlacements.Length];
            if (_tool == LayoutTool.Move)
            {
                var start = CanvasToLocal(_dragStartMouse, canvas);
                var current = CanvasToLocal(mousePosition, canvas);
                var delta = current - start;
                if (Event.current.shift)
                {
                    if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
                    {
                        delta.z = 0f;
                    }
                    else
                    {
                        delta.x = 0f;
                    }
                }

                if (_snapPosition)
                {
                    delta.x = Snap(delta.x, _positionSnap);
                    delta.z = Snap(delta.z, _positionSnap);
                }

                for (var i = 0; i < placements.Length; i++)
                {
                    placements[i] = _transformPlacements[i];
                    placements[i].position += new Vector3(delta.x, 0f, delta.z);
                }
            }
            else
            {
                var current = CanvasToLocal(mousePosition, canvas);
                var currentAngle = Mathf.Atan2(current.z - _transformPivot.z, current.x - _transformPivot.x) * Mathf.Rad2Deg;
                var angle = Mathf.DeltaAngle(_transformStartAngle, currentAngle);
                if (_snapAngle) angle = Snap(angle, _angleSnap);
                var rotation = Quaternion.Euler(0f, angle, 0f);

                for (var i = 0; i < placements.Length; i++)
                {
                    placements[i] = _transformPlacements[i];
                    var offset = placements[i].position - _transformPivot;
                    placements[i].position = _transformPivot + rotation * offset;
                    placements[i].eulerRotation.y += angle;
                }
            }

            session.SetBlockPlacements(_transformIndices, placements, _tool == LayoutTool.Move
                ? "Move Fanlight Blocks"
                : "Rotate Fanlight Blocks");
            Repaint();
        }

        private void BeginShape(int handle)
        {
            var active = FanlightLayoutSelection.GetActiveIndex(_layout);
            if (active < 0) return;

            _shapeHandle = handle;
            _shapeRows = CloneRows(_layout.GetBlock(active));
            BeginUndo("Shape Fanlight Block");
        }

        private void UpdateShape(Vector2 mousePosition, Rect canvas, FanlightLayoutEditSession session)
        {
            var active = FanlightLayoutSelection.GetActiveIndex(_layout);
            if (active < 0 || _shapeRows.Length == 0) return;

            var placement = _layout.GetBlock(active).Placement;
            var layoutPoint = CanvasToLocal(mousePosition, canvas);
            var inverseRotation = Quaternion.Inverse(placement.Rotation);
            var blockPoint = inverseRotation * (layoutPoint - placement.position);
            var first = _shapeRows[0];
            var last = _shapeRows[^1];
            var cage = new[] { first.LeftPoint, first.RightPoint, last.RightPoint, last.LeftPoint };
            var frontControl = first.ControlPoint;
            var backControl = last.ControlPoint;

            if (_shapeHandle < 4)
            {
                blockPoint.y = cage[_shapeHandle].y;
                cage[_shapeHandle] = blockPoint;
            }
            else if (_shapeHandle == 4)
            {
                blockPoint.y = frontControl.y;
                frontControl = blockPoint;
            }
            else
            {
                blockPoint.y = backControl.y;
                backControl = blockPoint;
            }

            var rows = new FanlightLayoutRow[_shapeRows.Length];
            var frontBulge = frontControl - (cage[0] + cage[1]) * 0.5f;
            var backBulge = backControl - (cage[3] + cage[2]) * 0.5f;
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var t = rows.Length == 1 ? 0.5f : (float)rowIndex / (rows.Length - 1);
                var source = _shapeRows[rowIndex];
                var left = Vector3.Lerp(cage[0], cage[3], t);
                var right = Vector3.Lerp(cage[1], cage[2], t);
                var control = (left + right) * 0.5f + Vector3.Lerp(frontBulge, backBulge, t);
                rows[rowIndex] = new FanlightLayoutRow(left, control, right, source.CopyStableSeatIds());
            }

            session.SetBlockRows(active, rows, "Shape Fanlight Block");
            Repaint();
        }

        private void CompleteMarquee(Rect canvas, FanlightLayoutEditSession session, bool additive)
        {
            var marquee = Rect.MinMaxRect(
                Mathf.Min(_dragStartMouse.x, _marqueeCurrent.x),
                Mathf.Min(_dragStartMouse.y, _marqueeCurrent.y),
                Mathf.Max(_dragStartMouse.x, _marqueeCurrent.x),
                Mathf.Max(_dragStartMouse.y, _marqueeCurrent.y));
            var indices = new List<int>();
            if (additive) FanlightLayoutSelection.GetIndices(_layout, indices);

            for (var blockIndex = 0; blockIndex < _layout.BlockCount; blockIndex++)
            {
                var bounds = GetCanvasBounds(session.GetCorners(blockIndex), canvas);
                if (marquee.Overlaps(bounds, true) && !indices.Contains(blockIndex)) indices.Add(blockIndex);
            }

            FanlightLayoutSelection.SetIndices(_layout, indices);
        }

        private void EndDrag()
        {
            _marquee = false;
            _marqueeAdditive = false;
            _transforming = false;
            _shapeHandle = -1;
            _pressedBlock = -1;
            _dragChanged = false;
            _transformIndices = Array.Empty<int>();
            _transformPlacements = Array.Empty<FanlightBlockPlacement>();
            _shapeRows = Array.Empty<FanlightLayoutRow>();
            if (_undoGroup >= 0)
            {
                Undo.CollapseUndoOperations(_undoGroup);
                _undoGroup = -1;
            }

            Repaint();
        }

        private void DrawAdvancedRows()
        {
            var blockIndex = FanlightLayoutSelection.GetActiveIndex(_layout);
            if (blockIndex < 0 || _selectedBlocks.Count != 1) return;

            var session = FanlightLayoutEditSession.Get(_layout);
            var block = _layout.GetBlock(blockIndex);
            var rowIndex = FanlightLayoutSelection.GetSelectedRowIndex(_layout);
            var row = block.GetRow(rowIndex);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Advanced Row Editing", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                var nextRow = EditorGUILayout.IntSlider("Row", rowIndex + 1, 1, block.RowCount) - 1;
                if (EditorGUI.EndChangeCheck())
                {
                    FanlightLayoutSelection.SetSelectedRowIndex(_layout, nextRow);
                    rowIndex = nextRow;
                    row = block.GetRow(rowIndex);
                }

                EditorGUI.BeginChangeCheck();
                var left = EditorGUILayout.Vector3Field("Left", row.LeftPoint);
                var control = EditorGUILayout.Vector3Field("Control", row.ControlPoint);
                var right = EditorGUILayout.Vector3Field("Right", row.RightPoint);
                if (EditorGUI.EndChangeCheck())
                {
                    session?.SetRowGeometry(blockIndex, rowIndex, left, control, right, "Edit Fanlight Row Geometry");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    var seatCount = Mathf.Clamp(EditorGUILayout.IntField("Seat Count", row.SeatCount), 1, 4096);
                    _seatAnchor = (FanlightLayoutGenerator.SeatAnchor)EditorGUILayout.EnumPopup(_seatAnchor, GUILayout.Width(80f));
                    if (EditorGUI.EndChangeCheck() && seatCount != row.SeatCount) ResizeRow(blockIndex, rowIndex, seatCount);

                    if (GUILayout.Button("Add")) AddRow(blockIndex, rowIndex);
                    using (new EditorGUI.DisabledScope(block.RowCount <= 1))
                    {
                        if (GUILayout.Button("Delete")) DeleteRow(blockIndex, rowIndex);
                    }

                    using (new EditorGUI.DisabledScope(rowIndex <= 0))
                    {
                        if (GUILayout.Button("Up")) MoveRow(blockIndex, rowIndex, rowIndex - 1);
                    }

                    using (new EditorGUI.DisabledScope(rowIndex >= block.RowCount - 1))
                    {
                        if (GUILayout.Button("Down")) MoveRow(blockIndex, rowIndex, rowIndex + 1);
                    }
                }
            }
        }

        private bool CreateQuickGrid(string undoName)
        {
            if (!TryGetQuickGridCounts(out _, out _)) return false;

            FanlightLayoutBlock[] blocks;
            var seatSpacing = new float2(_quickSeatSpacing.x, _quickSeatSpacing.y);

            try
            {
                blocks = FanlightLayoutGenerator.GenerateQuickGrid(
                    new int2(_quickBlockCount.x, _quickBlockCount.y),
                    new int2(_quickSeatsPerBlock.x, _quickSeatsPerBlock.y),
                    seatSpacing,
                    new float2(_quickAisleWidth.x, _quickAisleWidth.y));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Quick Grid Failed", exception.Message, "OK");
                return false;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var paletteTargets = CollectLayoutInstances(_layout);
            Undo.RecordObject(_layout, undoName);
            if (_layout.ActiveBake != null) Undo.RecordObject(_layout.ActiveBake, undoName);
            for (var i = 0; i < paletteTargets.Count; i++) Undo.RecordObject(paletteTargets[i], undoName);

            try
            {
                _layout.ReplaceAuthoringContents(seatSpacing, blocks);
                SynchronizeBaselineBlockPalettes(paletteTargets);
                EditorUtility.SetDirty(_layout);
                FanlightLayoutEditSession.Reset(_layout);
                FanlightLayoutIdRegistry.Invalidate();
                var session = FanlightLayoutEditSession.Get(_layout);
                if (session == null) throw new InvalidOperationException("The generated Layout is invalid.");

                if (!session.Bake())
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    RefreshAfterQuickGridRollback();
                    return false;
                }

                session.ApplyPreviewToAllInstances(-1);
                FanlightLayoutSelection.SetOnly(_layout, 0);
                FitView();
                Undo.CollapseUndoOperations(undoGroup);
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                RefreshAfterQuickGridRollback();
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Quick Grid Failed", exception.Message, "OK");
                return false;
            }
        }

        private void SetQuickGridSettings(
            Vector2Int blockCount,
            Vector2Int seatsPerBlock,
            Vector2 seatSpacing,
            Vector2 aisleWidth)
        {
            _quickBlockCount = blockCount;
            _quickSeatsPerBlock = seatsPerBlock;
            _quickSeatSpacing = seatSpacing;
            _quickAisleWidth = aisleWidth;
        }

        private void OpenQuickGridGenerator(Rect activatorRect)
        {
            if (_layout == null
                || !_layout.IsInitialized
                || Application.isPlaying
                || FanlightLayoutIdRegistry.IsDuplicate(_layout))
            {
                return;
            }

            PopupWindow.Show(activatorRect, new QuickGridPopupContent(this));
        }

        private void OpenBendPopup(Rect activatorRect)
        {
            FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);
            if (_selectedBlocks.Count < 2) return;

            PopupWindow.Show(activatorRect, new BendPopupContent(20f, BendSelected));
        }

        private void SynchronizeBaselineBlockPalettes(IReadOnlyList<PrismFanlight> fanlights)
        {
            for (var i = 0; i < fanlights.Count; i++)
            {
                var fanlight = fanlights[i];
                if (fanlight == null || fanlight.LayoutAsset != _layout) continue;

                var serializedFanlight = new SerializedObject(fanlight);
                serializedFanlight.Update();
                var color = serializedFanlight.FindProperty("_color");
                var source = color?.FindPropertyRelative("_source");
                var entries = source?.FindPropertyRelative("_blockPaletteEntries");
                if (entries == null) continue;

                FanlightColorIntensityEditorUtility.SynchronizeBlockPaletteEntries(entries, _layout);
                serializedFanlight.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(fanlight);
            }
        }

        private void RefreshAfterQuickGridRollback()
        {
            FanlightLayoutEditSession.Reset(_layout);
            FanlightLayoutIdRegistry.Invalidate();
            FanlightLayoutEditSession.Get(_layout)?.ApplyPreviewToAllInstances(-1);

            Repaint();
        }

        private static List<PrismFanlight> CollectLayoutInstances(FanlightLayoutAsset layout)
        {
            var results = new List<PrismFanlight>();
            var fanlights = FindObjectsByType<PrismFanlight>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (var i = 0; i < fanlights.Length; i++)
            {
                if (fanlights[i].LayoutAsset == layout) results.Add(fanlights[i]);
            }

            return results;
        }

        private void DuplicateSelected()
            => FanlightLayoutCommands.Duplicate(_layout);

        private void AddGeneratedBlock(FanlightLayoutGenerator.Shape shape, Vector3 layoutPosition)
            => FanlightLayoutCommands.AddBlock(_layout, shape, layoutPosition);

        private void DeleteSelected()
            => FanlightLayoutCommands.Delete(_layout);

        private void MirrorSelected()
            => FanlightLayoutCommands.Mirror(_layout);

        private void BendSelected(float bendDegrees)
            => FanlightLayoutCommands.Bend(_layout, bendDegrees);

        private void SnapActiveEdge()
            => FanlightLayoutCommands.SnapActiveEdge(_layout);

        private void AlignSelected(bool xAxis)
            => FanlightLayoutCommands.Align(_layout, xAxis);

        private void DistributeSelected(bool xAxis)
            => FanlightLayoutCommands.Distribute(_layout, xAxis);

        private void ResetSelected()
            => FanlightLayoutCommands.ResetPlacement(_layout);

        private void ShowCanvasMenu(Vector3 layoutPosition)
        {
            var popupRect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
            var menu = new GenericMenu();

            AddCreateItems(menu, "Create/", layoutPosition);

            menu.AddItem(new GUIContent("Layout/Regenerate"), false, () => OpenQuickGridGenerator(popupRect));
            menu.AddSeparator(string.Empty);

            FanlightLayoutSelection.GetIndices(_layout, _selectedBlocks);

            if (_selectedBlocks.Count > 0)
            {
                menu.AddItem(new GUIContent("Copy"), false, () => FanlightLayoutClipboard.Copy(_layout, _selectedBlocks.ToArray()));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            if (FanlightLayoutClipboard.CanPaste)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => FanlightLayoutClipboard.Paste(_layout, layoutPosition));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            if (_selectedBlocks.Count > 0)
            {
                menu.AddItem(new GUIContent("Duplicate"), false, DuplicateSelected);
                menu.AddItem(new GUIContent("Delete"), false, DeleteSelected);
                menu.AddItem(new GUIContent("Reset"), false, ResetSelected);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Mirror X"), false, MirrorSelected);
                menu.AddItem(new GUIContent("Edge Snap"), false, SnapActiveEdge);

                if (_selectedBlocks.Count >= 2)
                {
                    menu.AddItem(new GUIContent("Bend"), false, () => OpenBendPopup(popupRect));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Bend"));
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Duplicate"));
                menu.AddDisabledItem(new GUIContent("Delete"));
                menu.AddDisabledItem(new GUIContent("Reset"));
                menu.AddDisabledItem(new GUIContent("Bend"));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Align/X"), false, () => AlignSelected(true));
            menu.AddItem(new GUIContent("Align/Z"), false, () => AlignSelected(false));
            menu.AddItem(new GUIContent("Distribute/X"), false, () => DistributeSelected(true));
            menu.AddItem(new GUIContent("Distribute/Z"), false, () => DistributeSelected(false));
            menu.AddSeparator(string.Empty);

            menu.AddItem(
                new GUIContent("Advanced Row Editing"),
                FanlightLayoutSelection.IsAdvancedRowEditing(_layout),
                ToggleAdvancedRows);
            menu.ShowAsContext();
        }

        private void AddCreateItems(GenericMenu menu, string prefix, Vector3 layoutPosition)
        {
            menu.AddItem(new GUIContent($"{prefix}Rectangle"), false,
                () => AddGeneratedBlock(FanlightLayoutGenerator.Shape.Rectangle, layoutPosition));
            menu.AddItem(new GUIContent($"{prefix}Trapezoid"), false,
                () => AddGeneratedBlock(FanlightLayoutGenerator.Shape.Trapezoid, layoutPosition));
            menu.AddItem(new GUIContent($"{prefix}Fan"), false,
                () => AddGeneratedBlock(FanlightLayoutGenerator.Shape.Fan, layoutPosition));
            menu.AddItem(new GUIContent($"{prefix}Raked"), false,
                () => AddGeneratedBlock(FanlightLayoutGenerator.Shape.Raked, layoutPosition));
        }

        private void ResizeRow(int blockIndex, int rowIndex, int seatCount)
        {
            var block = _layout.GetBlock(blockIndex);
            var row = block.GetRow(rowIndex);
            var reserved = new HashSet<ulong>();
            _layout.CollectStableSeatIds(reserved);
            var rows = block.CopyRows();
            rows[rowIndex] = new FanlightLayoutRow(
                row.LeftPoint,
                row.ControlPoint,
                row.RightPoint,
                FanlightLayoutGenerator.ResizeStableSeatIds(row, seatCount, _seatAnchor, reserved));
            FanlightLayoutEditSession.ApplyTopologyChange(
                _layout,
                "Resize Fanlight Row",
                () => _layout.SetBlockRows(blockIndex, rows));
        }

        private void AddRow(int blockIndex, int rowIndex)
        {
            var block = _layout.GetBlock(blockIndex);
            var rows = new List<FanlightLayoutRow>(block.CopyRows());
            rows.Insert(rowIndex + 1, FanlightLayoutGenerator.CreateAdjacentRow(_layout, block, rowIndex));
            if (FanlightLayoutEditSession.ApplyTopologyChange(
                    _layout,
                    "Add Fanlight Row",
                    () => _layout.SetBlockRows(blockIndex, rows.ToArray())))
            {
                FanlightLayoutSelection.SetSelectedRowIndex(_layout, rowIndex + 1);
            }
        }

        private void DeleteRow(int blockIndex, int rowIndex)
        {
            var block = _layout.GetBlock(blockIndex);
            var rows = new List<FanlightLayoutRow>(block.CopyRows());
            rows.RemoveAt(rowIndex);
            if (FanlightLayoutEditSession.ApplyTopologyChange(
                    _layout,
                    "Delete Fanlight Row",
                    () => _layout.SetBlockRows(blockIndex, rows.ToArray())))
            {
                FanlightLayoutSelection.SetSelectedRowIndex(_layout, Mathf.Min(rowIndex, rows.Count - 1));
            }
        }

        private void MoveRow(int blockIndex, int sourceIndex, int destinationIndex)
        {
            var rows = _layout.GetBlock(blockIndex).CopyRows();
            (rows[sourceIndex], rows[destinationIndex]) = (rows[destinationIndex], rows[sourceIndex]);
            if (FanlightLayoutEditSession.ApplyTopologyChange(
                    _layout,
                    "Reorder Fanlight Rows",
                    () => _layout.SetBlockRows(blockIndex, rows)))
            {
                FanlightLayoutSelection.SetSelectedRowIndex(_layout, destinationIndex);
            }
        }

        private void SetTarget(PrismFanlight target)
        {
            var layout = target != null ? target.LayoutAsset : null;
            var layoutChanged = _layout != layout;
            _target = target;
            _layout = layout;
            ToolManager.RefreshAvailableTools();
            if (!layoutChanged)
            {
                Repaint();
                return;
            }

            _pan = Vector2.zero;
            _zoom = 42f;
            Repaint();
            if (_layout != null && _layout.IsInitialized)
            {
                if (FanlightLayoutSelection.GetActiveIndex(_layout) < 0) FanlightLayoutSelection.SetOnly(_layout, 0);
                EditorApplication.delayCall += FitView;
            }
        }

        private void UseCurrentSelection()
        {
            var gameObject = Selection.activeGameObject;
            if (gameObject != null && gameObject.TryGetComponent<PrismFanlight>(out var fanlight))
            {
                SetTarget(fanlight);
                return;
            }

            if (_target == null) SetTarget(null);
        }

        private void RefreshTargetLayout()
        {
            if (_target == null)
            {
                if (_locked) _locked = false;
                _layout = null;
                return;
            }

            var layout = _target.LayoutAsset;
            if (_layout != layout) SetTarget(_target);
        }

        private void ToggleLock()
        {
            if (!_locked && _target == null) UseCurrentSelection();
            _locked = !_locked && _target != null;
            if (!_locked) UseCurrentSelection();
            Repaint();
        }

        private void ToggleAdvancedRows()
        {
            if (_layout == null) return;

            var enabled = !FanlightLayoutSelection.IsAdvancedRowEditing(_layout);
            FanlightLayoutSelection.SetAdvancedRowEditing(
                _layout,
                enabled);
            if (enabled) ActivateLayoutTool();
        }

        private void ActivateLayoutTool()
        {
            if (Application.isPlaying
                || _target == null
                || _layout == null
                || !_layout.IsInitialized
                || FanlightLayoutSelection.GetActiveIndex(_layout) < 0)
            {
                return;
            }

            _activeWindow = this;
            if (ToolManager.activeToolType != typeof(FanlightLayoutTool))
            {
                ToolManager.SetActiveTool<FanlightLayoutTool>();
            }

            SceneView.RepaintAll();
        }

        private void ShowButton(Rect rect)
        {
            using (new EditorGUI.DisabledScope(_target == null))
            {
                EditorGUI.BeginChangeCheck();
                var next = GUI.Toggle(rect, _locked, GUIContent.none, "IN LockButton");
                if (!EditorGUI.EndChangeCheck() || next == _locked) return;
            }

            ToggleLock();
        }

        private void FitView()
        {
            if (_layout == null || !_layout.IsInitialized) return;

            var session = FanlightLayoutEditSession.Get(_layout);
            if (session == null) return;

            var bounds = session.RuntimeLayout.LocalBounds;
            var viewCenter = LocalToView(bounds.center);
            var viewSize = _canvasUpDirection is CanvasUpDirection.PositiveX or CanvasUpDirection.NegativeX
                ? new Vector2(bounds.size.z, bounds.size.x)
                : new Vector2(bounds.size.x, bounds.size.z);
            var available = new Vector2(Mathf.Max(240f, position.width - 60f), Mathf.Max(180f, position.height - 70f));
            _zoom = Mathf.Clamp(Mathf.Min(
                available.x / Mathf.Max(1f, viewSize.x),
                available.y / Mathf.Max(1f, viewSize.y)) * 0.82f, MinimumZoom, MaximumZoom);
            _pan = new Vector2(-viewCenter.x * _zoom, viewCenter.y * _zoom);
            Repaint();
        }

        private void UpdateTitle()
        {
            var session = _layout != null && _layout.IsInitialized
                ? FanlightLayoutEditSession.Get(_layout)
                : null;
            var bakeRequired = session != null && !session.HasCurrentBake;
            var text = bakeRequired ? $"{WindowTitle}*" : WindowTitle;
            var tooltip = bakeRequired ? $"{WindowTitle} - Bake required" : WindowTitle;
            if (titleContent.text == text && titleContent.tooltip == tooltip) return;

            titleContent = new GUIContent(text, titleContent.image, tooltip);
        }

        private int FindBlock(Vector2 mouse, Rect canvas, FanlightLayoutEditSession session)
        {
            for (var blockIndex = _layout.BlockCount - 1; blockIndex >= 0; blockIndex--)
            {
                var corners = session.GetCorners(blockIndex);
                var p0 = LocalToCanvas(corners[0], canvas);
                var p1 = LocalToCanvas(corners[1], canvas);
                var p2 = LocalToCanvas(corners[2], canvas);
                var p3 = LocalToCanvas(corners[3], canvas);
                if (PointInTriangle(mouse, p0, p1, p2) || PointInTriangle(mouse, p0, p2, p3)) return blockIndex;
            }

            return -1;
        }

        private int FindShapeHandle(Vector2 mouse, Rect canvas, FanlightLayoutEditSession session, int blockIndex)
        {
            var points = GetShapeHandlePoints(blockIndex, session);
            var best = -1;
            var bestDistance = ShapeHandleRadius * ShapeHandleRadius * 4f;
            for (var i = 0; i < points.Length; i++)
            {
                var distance = (LocalToCanvas(points[i], canvas) - mouse).sqrMagnitude;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = i;
            }

            return best;
        }

        private Vector3[] GetShapeHandlePoints(int blockIndex, FanlightLayoutEditSession session)
        {
            var corners = session.GetCorners(blockIndex);
            var block = _layout.GetBlock(blockIndex);
            var placement = block.Placement;
            return new[]
            {
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                placement.position + placement.Rotation * block.GetRow(0).ControlPoint,
                placement.position + placement.Rotation * block.GetRow(block.RowCount - 1).ControlPoint
            };
        }

        private Rect GetCanvasBounds(Vector3[] corners, Rect canvas)
        {
            var first = LocalToCanvas(corners[0], canvas);
            var min = first;
            var max = first;
            for (var i = 1; i < corners.Length; i++)
            {
                var point = LocalToCanvas(corners[i], canvas);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Vector2 LocalToCanvas(Vector3 local, Rect canvas)
            => ViewToCanvas(LocalToView(local), canvas);

        private Vector2 LocalToView(Vector3 local)
            => _canvasUpDirection switch
            {
                CanvasUpDirection.PositiveX => new Vector2(-local.z, local.x),
                CanvasUpDirection.NegativeZ => new Vector2(-local.x, -local.z),
                CanvasUpDirection.NegativeX => new Vector2(local.z, -local.x),
                _ => new Vector2(local.x, local.z)
            };

        private Vector2 ViewToCanvas(Vector2 viewPoint, Rect canvas)
            => canvas.center + _pan + new Vector2(viewPoint.x * _zoom, -viewPoint.y * _zoom);

        private Vector3 CanvasToLocal(Vector2 canvasPoint, Rect canvas)
            => ViewToLocal(CanvasToView(canvasPoint, canvas));

        private Vector3 ViewToLocal(Vector2 view)
            => _canvasUpDirection switch
            {
                CanvasUpDirection.PositiveX => new Vector3(view.y, 0f, -view.x),
                CanvasUpDirection.NegativeZ => new Vector3(-view.x, 0f, -view.y),
                CanvasUpDirection.NegativeX => new Vector3(-view.y, 0f, view.x),
                _ => new Vector3(view.x, 0f, view.y)
            };

        private Vector2 CanvasToView(Vector2 canvasPoint, Rect canvas)
        {
            var value = canvasPoint - canvas.center - _pan;
            return new Vector2(value.x / _zoom, -value.y / _zoom);
        }

        private void BeginUndo(string name)
        {
            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
        }

        private bool TryGetQuickGridCounts(out long totalBlocks, out long totalSeats)
            => TryGetQuickGridCounts(_quickBlockCount, _quickSeatsPerBlock, out totalBlocks, out totalSeats);

        private static bool TryGetQuickGridCounts(
            Vector2Int blockCount,
            Vector2Int seatsPerBlock,
            out long totalBlocks,
            out long totalSeats)
        {
            try
            {
                totalBlocks = checked((long)blockCount.x * blockCount.y);
                totalSeats = checked(totalBlocks * seatsPerBlock.x * seatsPerBlock.y);
                return totalBlocks is > 0 and <= int.MaxValue && totalSeats is > 0 and <= int.MaxValue;
            }
            catch (OverflowException)
            {
                totalBlocks = 0;
                totalSeats = 0;
                return false;
            }
        }

        private static FanlightLayoutRow[] CloneRows(FanlightLayoutBlock block)
        {
            var rows = new FanlightLayoutRow[block.RowCount];
            for (var i = 0; i < rows.Length; i++)
            {
                var row = block.GetRow(i);
                rows[i] = new FanlightLayoutRow(
                    row.LeftPoint,
                    row.ControlPoint,
                    row.RightPoint,
                    row.CopyStableSeatIds());
            }

            return rows;
        }

        private static float Snap(float value, float step)
            => Mathf.Round(value / Mathf.Max(0.0001f, step)) * step;

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = Cross(b - a, point - a);
            var bc = Cross(c - b, point - b);
            var ca = Cross(a - c, point - c);
            return ab >= 0f && bc >= 0f && ca >= 0f || ab <= 0f && bc <= 0f && ca <= 0f;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
