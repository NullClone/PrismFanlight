using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [EditorTool("Prism Fanlight Layout")]
    internal sealed class FanlightLayoutTool : EditorTool
    {
        // Fields

        private readonly List<int> _selectedBlocks = new();


        // Properties

        internal static bool IsActive => ToolManager.activeToolType == typeof(FanlightLayoutTool);

        public override GUIContent toolbarIcon
            => EditorGUIUtility.TrIconContent("MoveTool", "Edit Prism Fanlight block placement, height, and advanced rows.");


        // Methods

        public override void OnActivated()
        {
            SceneView.RepaintAll();
        }

        public override void OnWillBeDeactivated()
        {
            SceneView.RepaintAll();
        }

        public override bool IsAvailable()
            => CanEdit(ResolveTarget());

        public override void OnToolGUI(EditorWindow window)
        {
            var fanlight = ResolveTarget();
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

            FanlightLayoutScenePreview.DrawTransformHandle(
                fanlight,
                layout,
                session,
                _selectedBlocks,
                activeBlockIndex);
            FanlightLayoutScenePreview.DrawRiseHandle(
                fanlight,
                layout,
                session,
                _selectedBlocks,
                activeBlockIndex);

            if (FanlightLayoutSelection.IsAdvancedRowEditing(layout) && _selectedBlocks.Count == 1)
            {
                FanlightLayoutScenePreview.DrawRowsAndHandles(
                    fanlight,
                    layout,
                    session,
                    activeBlockIndex);
            }
        }

        private static PrismFanlight ResolveTarget()
        {
            var windowTarget = FanlightLayoutEditorWindow.ActiveTarget;
            if (windowTarget != null) return windowTarget;

            var gameObject = Selection.activeGameObject;
            return gameObject != null && gameObject.TryGetComponent<PrismFanlight>(out var fanlight)
                ? fanlight
                : null;
        }

        private static bool CanEdit(PrismFanlight fanlight)
        {
            var layout = fanlight != null ? fanlight.LayoutAsset : null;
            return !Application.isPlaying
                   && layout != null
                   && layout.IsInitialized
                   && !FanlightLayoutIdRegistry.IsDuplicate(layout)
                   && FanlightLayoutSelection.GetActiveIndex(layout) >= 0;
        }
    }
}
