using System.Collections.Generic;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

namespace PrismFanlight.Editor.Timeline
{
    [CustomEditor(typeof(FanlightTimelinePlayableAsset))]
    public sealed class FanlightTimelinePlayableAssetEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _colorSettings;
        private SerializedProperty _motionSettings;
        private SerializedProperty _tempoSettings;
        private SerializedProperty _audienceSettings;
        private SerializedProperty _paths;


        // Methods

        private void OnEnable()
        {
            serializedObject.Update();

            _colorSettings = serializedObject.FindProperty(nameof(_colorSettings));
            _motionSettings = serializedObject.FindProperty(nameof(_motionSettings));
            _tempoSettings = serializedObject.FindProperty(nameof(_tempoSettings));
            _audienceSettings = serializedObject.FindProperty(nameof(_audienceSettings));

            _paths = serializedObject.FindProperty("_overrides._paths");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGroup("Color", FanlightTimelineSettingsGroup.Color, _colorSettings);
            DrawGroup("Motion", FanlightTimelineSettingsGroup.Motion, _motionSettings);
            DrawGroup("Tempo", FanlightTimelineSettingsGroup.Tempo, _tempoSettings);
            DrawGroup("Audience", FanlightTimelineSettingsGroup.Audience, _audienceSettings);

            if (serializedObject.ApplyModifiedProperties())
            {
                RefreshTimelinePreview();
            }
        }

        private void DrawGroup(string title, FanlightTimelineSettingsGroup group, SerializedProperty root)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            var bySection = new Dictionary<string, List<FanlightTimelineOverrideDescriptor>>();

            foreach (var descriptor in FanlightTimelineOverrideSchema.GetGroup(group))
            {
                if (!bySection.TryGetValue(descriptor.DisplayGroup, out var descriptors))
                {
                    descriptors = new List<FanlightTimelineOverrideDescriptor>();
                    bySection.Add(descriptor.DisplayGroup, descriptors);
                }

                descriptors.Add(descriptor);
            }

            foreach (var pair in bySection)
            {
                DrawSection(pair.Key, pair.Value, root);
            }
        }

        private void DrawSection(string title, List<FanlightTimelineOverrideDescriptor> descriptors, SerializedProperty root)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(36))) SetAll(descriptors, true);
                if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(42))) SetAll(descriptors, false);
            }

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var descriptor in descriptors)
                {
                    var property = root.FindPropertyRelative(descriptor.RelativePath);
                    if (property == null) continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var enabled = Contains(descriptor.Path);
                        var next = EditorGUILayout.Toggle(enabled, GUILayout.Width(16));
                        if (next != enabled) Set(descriptor.Path, next);

                        using (new EditorGUI.DisabledScope(!next))
                        {
                            EditorGUILayout.PropertyField(property, new GUIContent(descriptor.DisplayName), property.isArray && property.propertyType != SerializedPropertyType.String);
                        }
                    }
                }
            }
        }

        private bool Contains(string path)
        {
            for (var i = 0; i < _paths.arraySize; i++)
            {
                if (_paths.GetArrayElementAtIndex(i).stringValue == path) return true;
            }

            return false;
        }

        private void SetAll(IEnumerable<FanlightTimelineOverrideDescriptor> descriptors, bool enabled)
        {
            foreach (var descriptor in descriptors) Set(descriptor.Path, enabled);
        }

        private void Set(string path, bool enabled)
        {
            for (var i = 0; i < _paths.arraySize; i++)
            {
                if (_paths.GetArrayElementAtIndex(i).stringValue != path) continue;

                if (!enabled)
                {
                    _paths.DeleteArrayElementAtIndex(i);
                }

                return;
            }

            if (!enabled) return;

            _paths.InsertArrayElementAtIndex(_paths.arraySize);
            _paths.GetArrayElementAtIndex(_paths.arraySize - 1).stringValue = path;
        }

        private static void RefreshTimelinePreview()
        {
            var director = TimelineEditor.inspectedDirector;

            if (director && !Application.isPlaying)
            {
                director.RebuildGraph();
                director.Evaluate();
            }

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
