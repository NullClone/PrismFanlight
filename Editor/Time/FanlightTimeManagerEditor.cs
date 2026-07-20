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
        private SerializedProperty _primaryMode;
        private SerializedProperty _primaryProvider;
        private SerializedProperty _manualSeconds;
        private SerializedProperty _manualRate;
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
            _primaryMode = serializedObject.FindProperty(nameof(_primaryMode));
            _primaryProvider = serializedObject.FindProperty(nameof(_primaryProvider));
            _manualSeconds = serializedObject.FindProperty(nameof(_manualSeconds));
            _manualRate = serializedObject.FindProperty(nameof(_manualRate));
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

            DrawClock();
            DrawTempo();

            serializedObject.ApplyModifiedProperties();
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        private void DrawClock()
        {
            EditorGUILayout.PropertyField(_primaryMode);

            using (new EditorGUI.IndentLevelScope())
            {
                var mode = (ShowTimePrimaryMode)_primaryMode.enumValueIndex;

                switch (mode)
                {
                    case ShowTimePrimaryMode.Manual:
                        DrawManualClock();
                        break;
                    case ShowTimePrimaryMode.Component:
                        DrawComponentClock();
                        break;
                }
            }

            EditorGUILayout.PropertyField(_negativeTimePolicy, new GUIContent("Negative Time"));
        }

        private void DrawManualClock()
        {
            EditorGUILayout.PropertyField(_manualSeconds, new GUIContent("Seconds"));
            EditorGUILayout.PropertyField(_manualRate, new GUIContent("Rate"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Time")) _manualSeconds.doubleValue = 0d;
                if (GUILayout.Button("Hold")) _manualRate.doubleValue = 0d;
                if (GUILayout.Button("Play Forward")) _manualRate.doubleValue = 1d;
            }

            EditorGUILayout.Space();
        }

        private void DrawComponentClock()
        {
            EditorGUILayout.PropertyField(_primaryProvider, new GUIContent("Provider"));
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
    }
}
