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
        defaultWidth = 370f,
        minWidth = 320f,
        maxWidth = 520f)]
    internal sealed class FanlightBlockOverlay : Overlay, ITransientOverlay
    {
        // Fields

        private SerializedObject _serializedFanlight;


        // Properties

        public bool visible => TryGetSelection(out _, out _, out _);


        // Methods

        public override VisualElement CreatePanelContent()
        {
            var content = new IMGUIContainer(DrawPanel);
            content.style.minWidth = 320f;
            return content;
        }

        public override void OnCreated()
        {
            Selection.selectionChanged += RepaintSceneView;
        }

        public override void OnWillBeDestroyed()
        {
            Selection.selectionChanged -= RepaintSceneView;
            _serializedFanlight = null;
        }


        private void DrawPanel()
        {
            if (!TryGetSelection(out var fanlight, out var layout, out var blockIndex)) return;

            var session = FanlightLayoutEditSession.Get(layout);
            if (session == null) return;

            if (_serializedFanlight == null || _serializedFanlight.targetObject != fanlight)
            {
                _serializedFanlight = new SerializedObject(fanlight);
            }

            _serializedFanlight.Update();

            var coordinates = layout.GetBlockCoordinates(blockIndex);
            var block = layout.GetBlock(blockIndex);
            var placement = block.Placement;
            EditorGUILayout.LabelField("Selected Block", $"{coordinates.x}, {coordinates.y}");
            var stableIdRect = EditorGUILayout.GetControlRect();
            stableIdRect = EditorGUI.PrefixLabel(stableIdRect, new GUIContent("ID"));
            EditorGUI.SelectableLabel(stableIdRect, block.BlockId, EditorStyles.textField);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                var position = EditorGUILayout.Vector3Field("Position", placement.position);
                var rotation = EditorGUILayout.Vector3Field("Rotation", placement.eulerRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    session.SetBlockPlacement(
                        blockIndex,
                        new FanlightBlockPlacement
                        {
                            position = position,
                            eulerRotation = rotation
                        },
                        "Edit Fanlight Block Placement");
                }

                if (GUILayout.Button("Reset Selected Block"))
                {
                    session.SetBlockPlacement(
                        blockIndex,
                        FanlightBlockPlacement.Identity,
                        "Reset Fanlight Block Placement");
                }
            }

            var colorState = _serializedFanlight.FindProperty("_color");
            if (FanlightColorIntensityEditorUtility.IsBlockPalette(colorState))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
                FanlightColorIntensityEditorUtility.DrawSelectedBlockColor(
                    colorState,
                    layout,
                    blockIndex);
            }

            if (_serializedFanlight.ApplyModifiedProperties())
            {
                SceneView.RepaintAll();
            }
        }

        private void RepaintSceneView()
        {
            if (containerWindow is SceneView sceneView)
            {
                sceneView.Repaint();
            }
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
            if (layout == null || !layout.IsInitialized || FanlightLayoutIdRegistry.IsDuplicate(layout))
            {
                return false;
            }

            blockIndex = FanlightLayoutScenePreview.GetSelectedBlockIndex(layout);
            return blockIndex >= 0;
        }
    }
}
