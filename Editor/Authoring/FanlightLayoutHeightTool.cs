using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [EditorTool("Prism Fanlight Height", typeof(PrismFanlight))]
    internal sealed class FanlightLayoutHeightTool : EditorTool
    {
        // Fields

        private readonly List<int> _selectedBlocks = new();


        // Properties

        internal static bool IsActive => ToolManager.activeToolType == typeof(FanlightLayoutHeightTool);

        public override GUIContent toolbarIcon
            => EditorGUIUtility.TrIconContent("ScaleTool", "Edit Prism Fanlight block lift and rise.");


        // Methods

        public override void OnActivated()
        {
            SceneView.RepaintAll();
        }

        public override void OnWillBeDeactivated()
        {
            SceneView.RepaintAll();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var fanlight = target as PrismFanlight;
            if (window is not SceneView
                || !FanlightLayoutScenePreview.TryGetToolContext(
                    fanlight,
                    _selectedBlocks,
                    out var layout,
                    out var session,
                    out var activeBlockIndex))
            {
                return;
            }

            FanlightLayoutScenePreview.DrawHeightHandles(
                fanlight,
                layout,
                session,
                _selectedBlocks,
                activeBlockIndex);
        }
    }
}
