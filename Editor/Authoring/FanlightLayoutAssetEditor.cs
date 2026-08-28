using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FanlightLayoutAsset))]
    internal sealed class FanlightLayoutAssetEditor : UnityEditor.Editor
    {
        // Fields

        private FanlightLayoutAsset _instance;


        // Methods

        private void OnEnable()
        {
            _instance = target as FanlightLayoutAsset;
        }

        public override void OnInspectorGUI()
        {
            if (_instance == null) return;

            if (!_instance.IsInitialized) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total Blocks", _instance.BlockCount.ToString("N0"));
            EditorGUILayout.LabelField("Total Seats", _instance.TotalSeatCount.ToString("N0"));
            EditorGUILayout.LabelField(
                "Reference Seat Spacing",
                $"{_instance.ReferenceSeatSpacing.x:0.###}, {_instance.ReferenceSeatSpacing.y:0.###}");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stable Identity", _instance.LayoutId.Value);
            EditorGUILayout.LabelField("Content Hash", _instance.ContentHash.ToString("X16"));
            EditorGUILayout.Space();

            if (FanlightLayoutIdRegistry.IsDuplicate(_instance))
            {
                EditorGUILayout.HelpBox("Duplicate Layout ID detected. Rendering and baking are disabled.", MessageType.Error);
                EditorGUILayout.Space();
                return;
            }

            var session = FanlightLayoutEditSession.Get(_instance);
            if (session == null) return;

            using (new EditorGUI.DisabledScope(session.HasCurrentBake || Application.isPlaying || serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("Bake Layout"))
                {
                    session.Bake();
                }
            }
        }
    }
}
