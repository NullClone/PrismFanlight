using PrismFanlight.Time;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightTimeManager))]
    internal sealed class FanlightTimeManagerEditor : UnityEditor.Editor
    {
        // Fields

        private FanlightTimeManager _instance;
        private SerializedProperty _negativeTimePolicy;
        private SerializedProperty _provider;
        private SerializedProperty _defaultBpm;
        private SerializedProperty _defaultBeatsPerBar;
        private SerializedProperty _defaultBeatUnit;
        private SerializedProperty _defaultMusicalOriginSeconds;


        // Methods

        private void OnEnable()
        {
            _instance = target as FanlightTimeManager;

            if (_instance == null) return;

            _negativeTimePolicy = serializedObject.FindProperty(nameof(_negativeTimePolicy));
            _provider = serializedObject.FindProperty(nameof(_provider));
            _defaultBpm = serializedObject.FindProperty(nameof(_defaultBpm));
            _defaultBeatsPerBar = serializedObject.FindProperty(nameof(_defaultBeatsPerBar));
            _defaultBeatUnit = serializedObject.FindProperty(nameof(_defaultBeatUnit));
            _defaultMusicalOriginSeconds = serializedObject.FindProperty(nameof(_defaultMusicalOriginSeconds));
        }

        public override void OnInspectorGUI()
        {
            if (_instance == null) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(_negativeTimePolicy, new GUIContent("Negative Time"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_provider);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_defaultBpm, new GUIContent("BPM"));
            EditorGUILayout.PropertyField(_defaultBeatsPerBar, new GUIContent("Beats Per Bar"));
            EditorGUILayout.PropertyField(_defaultBeatUnit, new GUIContent("Beat Unit"));
            EditorGUILayout.PropertyField(_defaultMusicalOriginSeconds, new GUIContent("Musical Origin Seconds"));

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                var fallbackActive = _instance.IsFallbackActive;
                var primaryAvailable = _instance.IsPrimaryAvailable;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Fallback Active", fallbackActive ? "Yes" : "No");
                EditorGUILayout.LabelField("Primary Available", primaryAvailable ? "Yes" : "No");
                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(!fallbackActive || !primaryAvailable))
                {
                    if (GUILayout.Button("Reacquire Primary"))
                    {
                        _instance.TryRequestPrimaryReacquire(out _);
                    }
                }
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;
    }
}
