using PrismFanlight.Authoring;
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
        private SerializedProperty _tempoMap;
        private SerializedProperty _defaultBpm;
        private SerializedProperty _defaultBeatsPerBar;
        private SerializedProperty _defaultBeatUnit;
        private SerializedProperty _defaultOffsetSeconds;

        private static readonly string[] BeatUnitLabels = { "1", "2", "4", "8", "16" };
        private static readonly int[] BeatUnitValues = { 1, 2, 4, 8, 16 };


        // Methods

        private void OnEnable()
        {
            _instance = target as FanlightTimeManager;

            if (_instance == null) return;

            _negativeTimePolicy = serializedObject.FindProperty(nameof(_negativeTimePolicy));
            _provider = serializedObject.FindProperty(nameof(_provider));
            _tempoMap = serializedObject.FindProperty(nameof(_tempoMap));
            _defaultBpm = serializedObject.FindProperty(nameof(_defaultBpm));
            _defaultBeatsPerBar = serializedObject.FindProperty(nameof(_defaultBeatsPerBar));
            _defaultBeatUnit = serializedObject.FindProperty(nameof(_defaultBeatUnit));
            _defaultOffsetSeconds = serializedObject.FindProperty(nameof(_defaultOffsetSeconds));
        }

        public override void OnInspectorGUI()
        {
            if (_instance == null) return;

            serializedObject.Update();

            DrawProviderClock();
            DrawTempo();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                DrawPrimaryRecovery();
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        private void DrawProviderClock()
        {
            EditorGUILayout.PropertyField(_provider);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_negativeTimePolicy, new GUIContent("Negative Time"));
            EditorGUILayout.Space();
        }

        private void DrawTempo()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_tempoMap);

            var tempoMap = _tempoMap.objectReferenceValue as FanlightTempoMap;

            if (tempoMap != null)
            {
                if (!tempoMap.Validate(out var error))
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }

                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_defaultBpm, new GUIContent("BPM"));
                EditorGUILayout.PropertyField(_defaultBeatsPerBar, new GUIContent("Beats Per Bar"));
                _defaultBeatUnit.intValue = EditorGUILayout.IntPopup(
                    "Beat Unit",
                    _defaultBeatUnit.intValue,
                    BeatUnitLabels,
                    BeatUnitValues);
                EditorGUILayout.PropertyField(_defaultOffsetSeconds, new GUIContent("Offset Seconds"));
            }
        }

        private void DrawPrimaryRecovery()
        {
            var fallbackActive = _instance.IsFallbackActive;
            var primaryAvailable = _instance.IsPrimaryAvailable;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fallback Active", fallbackActive ? "Yes" : "No");
            EditorGUILayout.LabelField("Primary Available", primaryAvailable ? "Yes" : "No");

            using (new EditorGUI.DisabledScope(!fallbackActive || !primaryAvailable))
            {
                if (GUILayout.Button("Reacquire Primary"))
                {
                    _instance.TryRequestPrimaryReacquire(out _);
                }
            }
        }
    }
}
