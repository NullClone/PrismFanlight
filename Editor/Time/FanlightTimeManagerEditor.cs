using System;
using System.Collections.Generic;
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
        private SerializedProperty _primaryProvider;
        private SerializedProperty _tempoMap;
        private SerializedProperty _defaultBpm;
        private SerializedProperty _defaultBeatsPerBar;
        private SerializedProperty _defaultBeatUnit;
        private SerializedProperty _defaultOffsetSeconds;

        private static readonly string[] BeatUnitLabels = { "1", "2", "4", "8", "16" };
        private static readonly int[] BeatUnitValues = { 1, 2, 4, 8, 16 };

        private readonly List<Type> _providerTypes = new();
        private string[] _providerLabels;


        // Methods

        private void OnEnable()
        {
            _instance = target as FanlightTimeManager;

            if (_instance == null) return;

            _negativeTimePolicy = serializedObject.FindProperty(nameof(_negativeTimePolicy));
            _primaryProvider = serializedObject.FindProperty(nameof(_primaryProvider));
            _tempoMap = serializedObject.FindProperty(nameof(_tempoMap));
            _defaultBpm = serializedObject.FindProperty(nameof(_defaultBpm));
            _defaultBeatsPerBar = serializedObject.FindProperty(nameof(_defaultBeatsPerBar));
            _defaultBeatUnit = serializedObject.FindProperty(nameof(_defaultBeatUnit));
            _defaultOffsetSeconds = serializedObject.FindProperty(nameof(_defaultOffsetSeconds));

            RefreshProviderTypes();
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

        private void RefreshProviderTypes()
        {
            _providerTypes.Clear();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IShowTimeProvider>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition || !typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    continue;
                }

                _providerTypes.Add(type);
            }

            _providerTypes.Sort((left, right) => string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));

            _providerLabels = new string[_providerTypes.Count + 1];
            _providerLabels[0] = "None";

            for (int i = 0; i < _providerTypes.Count; i++)
            {
                _providerLabels[i + 1] = ObjectNames.NicifyVariableName(_providerTypes[i].Name);
            }
        }

        private void DrawProviderClock()
        {
            var provider = _primaryProvider.objectReferenceValue as MonoBehaviour;
            var selectedIndex = FindProviderTypeIndex(provider?.GetType());

            EditorGUI.BeginChangeCheck();
            var nextIndex = EditorGUILayout.Popup("Provider", selectedIndex, _providerLabels);
            if (EditorGUI.EndChangeCheck())
            {
                AssignProvider(nextIndex);

                provider = _primaryProvider.objectReferenceValue as MonoBehaviour;
                selectedIndex = FindProviderTypeIndex(provider?.GetType());
            }

            if (_providerTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No available time providers were found.", MessageType.Error);
            }

            if (provider == null)
            {
                EditorGUILayout.HelpBox("A Primary Provider is required. Runtime evaluation remains faulted until one is assigned.", MessageType.Error);
            }
            else if (selectedIndex < 0)
            {
                EditorGUILayout.HelpBox($"{provider.GetType().Name} is not an available time provider.", MessageType.Error);
            }

            EditorGUILayout.PropertyField(_negativeTimePolicy, new GUIContent("Negative Time"));
            EditorGUILayout.Space();
        }

        private int FindProviderTypeIndex(Type providerType)
        {
            if (providerType == null) return 0;

            for (var i = 0; i < _providerTypes.Count; i++)
            {
                if (_providerTypes[i] == providerType) return i + 1;
            }

            return -1;
        }

        private void AssignProvider(int selectedIndex)
        {
            if (selectedIndex == 0)
            {
                _primaryProvider.objectReferenceValue = null;
                return;
            }

            var providerTypeIndex = selectedIndex - 1;
            if (providerTypeIndex < 0 || providerTypeIndex >= _providerTypes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            }

            var providerType = _providerTypes[providerTypeIndex];
            var provider = _instance.GetComponent(providerType) as MonoBehaviour;

            if (provider == null)
            {
                provider = Undo.AddComponent(_instance.gameObject, providerType) as MonoBehaviour;
            }

            _primaryProvider.objectReferenceValue = provider;
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
