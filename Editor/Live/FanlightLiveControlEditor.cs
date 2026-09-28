using PrismFanlight.Live;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightLiveControl))]
    internal sealed class FanlightLiveControlEditor : UnityEditor.Editor
    {
        // Fields

        private FanlightLiveControl _instance;

        private SerializedProperty _looks;


        // Methods

        private void OnEnable()
        {
            _instance = target as FanlightLiveControl;

            _looks = serializedObject.FindProperty(nameof(_looks));
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            if (!_instance) return;

            serializedObject.Update();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("New Look", GUILayout.Width(90)))
                    {
                        CreateLook();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_looks, new GUIContent("Looks"));

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                DrawLiveStatus();
            }
        }

        private void DrawLiveStatus()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            var masterIntensity = EditorGUILayout.Slider("Master Intensity", _instance.MasterIntensity, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _instance.SetMasterIntensity(masterIntensity);
            }

            EditorGUI.BeginChangeCheck();
            var blackout = EditorGUILayout.Toggle("Blackout", _instance.Blackout);
            if (EditorGUI.EndChangeCheck())
            {
                _instance.SetBlackout(blackout);
            }

            EditorGUILayout.Space();

            var looks = _instance.Looks;

            for (var i = 0; i < looks.Count; i++)
            {
                var look = looks[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(i.ToString(), look != null ? look.name : "None");

                    using (new EditorGUI.DisabledScope(look == null))
                    {
                        if (GUILayout.Button("Request", GUILayout.Width(70)))
                        {
                            _instance.RequestLookAt(i);
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            var pending = _instance.PendingLook;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Pending", pending != null ? $"{pending.name} ({_instance.PendingQuantize})" : "None");

                using (new EditorGUI.DisabledScope(pending == null))
                {
                    if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                    {
                        _instance.CancelPendingLook();
                    }
                }
            }

            var lastApplied = _instance.LastAppliedLook;
            EditorGUILayout.LabelField("Last Applied", lastApplied != null ? lastApplied.name : "None");

            var status = _instance.Status;
            EditorGUILayout.LabelField("Rendering", status.IsRendering ? "Yes" : "No");

            if (status.IsHolding)
            {
                EditorGUILayout.HelpBox("Holding the last valid state because of a fault.", MessageType.Warning);
            }

            if (status.IsTimeFallbackActive)
            {
                EditorGUILayout.HelpBox("Time Fallback is active.", MessageType.Warning);
            }

            DrawFault("Time Fault", status.TimeFault);
            DrawFault("Sequence Fault", status.SequenceFault);
            DrawFault("Renderer Fault", status.RendererFault);
            DrawFault("Live Fault", _instance.Fault);
        }

        private static void DrawFault(string label, string fault)
        {
            if (string.IsNullOrEmpty(fault)) return;

            EditorGUILayout.HelpBox($"{label}: {fault}", MessageType.Error);
        }

        private void CreateLook()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Live Look",
                "New Fanlight Live Look",
                "asset",
                "Choose where to save the Live Look asset.");

            if (string.IsNullOrEmpty(path)) return;

            var look = CreateInstance<FanlightLiveLook>();

            AssetDatabase.CreateAsset(look, path);
            AssetDatabase.SaveAssets();

            serializedObject.Update();
            _looks.arraySize++;
            _looks.GetArrayElementAtIndex(_looks.arraySize - 1).objectReferenceValue = look;
            serializedObject.ApplyModifiedProperties();

            Selection.activeObject = look;
        }
    }
}
