using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrismFanlight.Editor
{
    [Overlay(
        typeof(SceneView),
        "Prism Fanlight/Block",
        "Block",
        defaultDisplay = false,
        defaultDockZone = DockZone.RightColumn,
        defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 0,
        defaultWidth = 390f,
        minWidth = 340f,
        maxWidth = 560f)]
    internal sealed class FanlightBlockOverlay : Overlay, ITransientOverlay
    {
        // Fields

        private readonly List<int> _selectedBlocks = new();
        private SerializedObject _serializedFanlight;


        // Properties

        public bool visible => TryGetSelection(out _, out _, out _);


        // Methods

        public override VisualElement CreatePanelContent()
        {
            var content = new IMGUIContainer(DrawPanel);
            content.style.minWidth = 340f;
            return content;
        }

        public override void OnCreated()
        {
            Selection.selectionChanged += RepaintSceneView;
            FanlightLayoutSelection.Changed += RepaintSceneView;
        }

        public override void OnWillBeDestroyed()
        {
            Selection.selectionChanged -= RepaintSceneView;
            FanlightLayoutSelection.Changed -= RepaintSceneView;
            _serializedFanlight = null;
        }


        private void DrawPanel()
        {
            if (!TryGetSelection(out var fanlight, out var layoutAsset, out var blockIndex)) return;

            var session = FanlightLayoutEditSession.Get(layoutAsset);
            if (session == null) return;

            FanlightLayoutSelection.GetIndices(layoutAsset, _selectedBlocks);

            if (_selectedBlocks.Count == 0) return;

            if (_serializedFanlight == null || _serializedFanlight.targetObject != fanlight)
            {
                _serializedFanlight = new SerializedObject(fanlight);
            }

            _serializedFanlight.Update();

            var block = layoutAsset.GetBlock(blockIndex);

            EditorGUILayout.LabelField("Selection", $"{_selectedBlocks.Count} Blocks");

            var hasMultiple = _selectedBlocks.Count > 1;

            DrawCopyableField("Stable Block ID", hasMultiple ? null : block.BlockId);
            DrawCopyableField("Rows", hasMultiple ? null : block.RowCount.ToString("N0"));
            DrawCopyableField("Position", hasMultiple ? null : Format(block.Placement.position));
            DrawCopyableField("Rotation", hasMultiple ? null : Format(block.Placement.eulerRotation));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Open Layout Editor"))
                    {
                        FanlightLayoutEditorWindow.Open(fanlight);
                    }
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying || session.HasCurrentBake))
                {
                    if (GUILayout.Button("Bake Layout")) session.Bake();
                }
            }

            /*
            if (FanlightLayoutTool.IsActive)
            {
                FanlightLayoutHeightUtility.GetHeights(layoutAsset, blockIndex, out var frontHeight, out var backHeight);
                var placementY = block.Placement.position.y;
                var rise = backHeight - frontHeight;
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    EditorGUI.BeginChangeCheck();
                    var nextPlacementY = EditorGUILayout.FloatField("Placement Y", placementY);
                    if (EditorGUI.EndChangeCheck())
                    {
                        FanlightLayoutHeightUtility.Lift(
                            layoutAsset,
                            session,
                            _selectedBlocks,
                            nextPlacementY - placementY);
                    }

                    EditorGUILayout.LabelField("Front Y", frontHeight.ToString("0.###"));
                    EditorGUILayout.LabelField("Back Y", backHeight.ToString("0.###"));
                    EditorGUI.BeginChangeCheck();
                    var nextRise = EditorGUILayout.FloatField("Rise", rise);
                    if (EditorGUI.EndChangeCheck())
                    {
                        FanlightLayoutHeightUtility.AddRise(
                            layoutAsset,
                            session,
                            _selectedBlocks,
                            nextRise - rise);
                    }

                    if (GUILayout.Button("Flatten"))
                    {
                        FanlightLayoutHeightUtility.Flatten(layoutAsset, session, _selectedBlocks);
                    }
                }
            }
            */

            EditorGUILayout.Space();

            var colorState = _serializedFanlight.FindProperty("_color");

            if (FanlightColorIntensityEditorUtility.IsBlockPalette(colorState))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Block Palette", EditorStyles.boldLabel);
                FanlightColorIntensityEditorUtility.DrawSelectedBlockColor(colorState, layoutAsset, blockIndex);
            }

            if (_serializedFanlight.ApplyModifiedProperties()) SceneView.RepaintAll();
        }

        private void RepaintSceneView()
        {
            if (containerWindow is SceneView sceneView)
            {
                sceneView.Repaint();
            }
        }

        private static void DrawCopyableField<T>(string label, T value)
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            EditorGUI.SelectableLabel(rect, value?.ToString() ?? "ー", EditorStyles.textField);
        }

        private static bool TryGetSelection(out PrismFanlight fanlight, out FanlightLayoutAsset layout, out int blockIndex)
        {
            fanlight = null;
            layout = null;
            blockIndex = -1;

            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length != 1) return false;

            fanlight = selectedObjects[0].GetComponent<PrismFanlight>();
            if (fanlight == null) return false;

            layout = fanlight.LayoutAsset;
            if (layout == null || !layout.IsInitialized || FanlightLayoutIdRegistry.IsDuplicate(layout)) return false;

            blockIndex = FanlightLayoutSelection.GetActiveIndex(layout);
            return blockIndex >= 0;
        }

        private static string Format(Vector3 value) => $"{value.x:0.###}, {value.y:0.###}, {value.z:0.###}";
    }
}
