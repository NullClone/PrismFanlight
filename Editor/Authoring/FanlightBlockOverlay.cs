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
            DrawPlacementFields(session, layoutAsset, blockIndex, _selectedBlocks);

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
                FanlightColorIntensityEditorUtility.DrawSelectedBlockColor(
                    colorState,
                    layoutAsset,
                    _selectedBlocks,
                    blockIndex);
            }

            var intensityState = _serializedFanlight.FindProperty("_intensity");

            if (FanlightColorIntensityEditorUtility.IsBlockAlternatingPulse(intensityState))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Block Alternating Pulse", EditorStyles.boldLabel);
                FanlightColorIntensityEditorUtility.DrawSelectedBlockPulseGroup(
                    intensityState,
                    layoutAsset,
                    blockIndex);
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
            EditorGUI.showMixedValue = value == null;
            EditorGUI.SelectableLabel(rect, value?.ToString(), EditorStyles.textField);
            EditorGUI.showMixedValue = false;
        }

        private static void DrawPlacementFields(
            FanlightLayoutEditSession session,
            FanlightLayoutAsset layout,
            int activeBlockIndex,
            IReadOnlyList<int> blockIndices)
        {
            var activePlacement = layout.GetBlock(activeBlockIndex).Placement;

            EditorGUI.showMixedValue = HasMixedPosition(layout, blockIndices, activePlacement.position);
            EditorGUI.BeginChangeCheck();
            var position = EditorGUILayout.Vector3Field("Position", activePlacement.position);
            var positionChanged = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;
            if (positionChanged)
            {
                ApplyPosition(session, layout, blockIndices, position);
            }

            EditorGUI.showMixedValue = HasMixedRotation(layout, blockIndices, activePlacement.eulerRotation);
            EditorGUI.BeginChangeCheck();
            var rotation = EditorGUILayout.Vector3Field("Rotation", activePlacement.eulerRotation);
            var rotationChanged = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;
            if (rotationChanged)
            {
                ApplyRotation(session, layout, blockIndices, rotation);
            }
        }

        private static bool HasMixedPosition(
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            Vector3 activePosition)
        {
            for (var i = 0; i < blockIndices.Count; i++)
            {
                if (!layout.GetBlock(blockIndices[i]).Placement.position.Equals(activePosition)) return true;
            }

            return false;
        }

        private static bool HasMixedRotation(
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            Vector3 activeRotation)
        {
            for (var i = 0; i < blockIndices.Count; i++)
            {
                if (!layout.GetBlock(blockIndices[i]).Placement.eulerRotation.Equals(activeRotation)) return true;
            }

            return false;
        }

        private static void ApplyPosition(
            FanlightLayoutEditSession session,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            Vector3 position)
        {
            var placements = new FanlightBlockPlacement[blockIndices.Count];
            for (var i = 0; i < placements.Length; i++)
            {
                placements[i] = layout.GetBlock(blockIndices[i]).Placement;
                placements[i].position = position;
            }

            session.SetBlockPlacements(
                blockIndices,
                placements,
                "Edit Fanlight Block Positions");
        }

        private static void ApplyRotation(
            FanlightLayoutEditSession session,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            Vector3 rotation)
        {
            var placements = new FanlightBlockPlacement[blockIndices.Count];
            for (var i = 0; i < placements.Length; i++)
            {
                placements[i] = layout.GetBlock(blockIndices[i]).Placement;
                placements[i].eulerRotation = rotation;
            }

            session.SetBlockPlacements(
                blockIndices,
                placements,
                "Edit Fanlight Block Rotations");
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
