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
            FanlightLayoutSelection.Changed += RepaintSceneView;
        }

        public override void OnWillBeDestroyed()
        {
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
            if (hasMultiple)
            {
                DrawCopyableField<string>("Position", null);
                DrawCopyableField<string>("Rotation", null);
            }
            else
            {
                DrawPlacementFields(session, blockIndex, block.Placement);
            }

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

        private static void DrawPlacementFields(
            FanlightLayoutEditSession session,
            int blockIndex,
            FanlightBlockPlacement placement)
        {
            EditorGUI.BeginChangeCheck();
            var position = EditorGUILayout.Vector3Field("Position", placement.position);
            var rotation = EditorGUILayout.Vector3Field("Rotation", placement.eulerRotation);
            if (!EditorGUI.EndChangeCheck()) return;

            placement.position = position;
            placement.eulerRotation = rotation;
            session.SetBlockPlacements(
                new[] { blockIndex },
                new[] { placement },
                "Edit Fanlight Block Placement");
        }

        private static bool TryGetSelection(out PrismFanlight fanlight, out FanlightLayoutAsset layout, out int blockIndex)
        {
            fanlight = null;
            layout = null;
            blockIndex = -1;

            if (!FanlightLayoutEditorWindow.TryGetActiveSceneContext(out fanlight, out layout)) return false;

            blockIndex = FanlightLayoutSelection.GetActiveIndex(layout);
            return blockIndex >= 0;
        }
    }
}
