using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [EditorTool("Prism Fanlight Layout", typeof(PrismFanlight))]
    internal sealed class FanlightLayoutTool : EditorTool
    {
        // Fields

        private readonly List<int> _selectedBlocks = new();


        // Properties

        public override GUIContent toolbarIcon
            => EditorGUIUtility.TrIconContent("MoveTool", "Edit Prism Fanlight block placement and advanced rows.");


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

            if (FanlightLayoutSelection.IsAdvancedRowEditing(layout) && _selectedBlocks.Count == 1)
            {
                FanlightLayoutScenePreview.DrawRowsAndHandles(
                    fanlight,
                    layout,
                    session,
                    activeBlockIndex);
                return;
            }

            FanlightLayoutScenePreview.DrawTransformHandle(
                fanlight,
                layout,
                session,
                _selectedBlocks,
                activeBlockIndex);
        }
    }
}
