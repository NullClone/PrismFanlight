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

            _providerLabels = new string[_providerTypes.Count];

            for (int i = 0; i < _providerTypes.Count; i++)
            {
                _providerLabels[i] = ObjectNames.NicifyVariableName(_providerTypes[i].Name);
            }
        }

        private void DrawProviderClock()
        {
            var provider = _primaryProvider.objectReferenceValue as MonoBehaviour;

            int selectedIndex;

            if (provider == null)
            {
                selectedIndex = FindProviderTypeIndex(typeof(UnityTimeProvider));

                AssignProvider(selectedIndex);

                provider = _primaryProvider.objectReferenceValue as MonoBehaviour;

                if (provider == null)
                {
                    throw new InvalidOperationException("Primary provider is null.");
                }
            }

            selectedIndex = FindProviderTypeIndex(provider.GetType());

            EditorGUI.BeginChangeCheck();
            var nextIndex = EditorGUILayout.Popup("Provider", selectedIndex, _providerLabels);
            if (EditorGUI.EndChangeCheck())
            {
                AssignProvider(nextIndex);

                provider = _primaryProvider.objectReferenceValue as MonoBehaviour;
                selectedIndex = FindProviderTypeIndex(provider.GetType());
            }

            if (_providerTypes.Count == 0)
            {
                throw new InvalidOperationException("No available time providers found.");
            }

            if (provider != null && selectedIndex < 0)
            {
                EditorGUILayout.HelpBox($"{provider.GetType().Name} is not an available time provider.", MessageType.Error);
            }

            EditorGUILayout.PropertyField(_negativeTimePolicy, new GUIContent("Negative Time"));
            EditorGUILayout.Space();
        }

        private int FindProviderTypeIndex(Type providerType)
        {
            for (var i = 0; i < _providerTypes.Count; i++)
            {
                if (_providerTypes[i] == providerType) return i;
            }

            throw new InvalidOperationException($"Provider type {providerType?.FullName ?? "null"} is not in the list of available providers.");
        }

        private void AssignProvider(int selectedIndex)
        {
            var providerType = selectedIndex < 0 ? typeof(UnityTimeProvider) : _providerTypes[selectedIndex];
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
