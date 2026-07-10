using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightTimelinePlayableAsset))]
    public sealed class FanlightTimelinePlayableAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _overrideColor;
        private SerializedProperty _colorSettings;
        private SerializedProperty _overrideMotion;
        private SerializedProperty _motion;
        private SerializedProperty _overrideTempo;
        private SerializedProperty _tempo;
        private SerializedProperty _overrideAudience;
        private SerializedProperty _audience;


        private void OnEnable()
        {
            _overrideColor = serializedObject.FindProperty("overrideColor");
            _colorSettings = serializedObject.FindProperty("colorSettings");
            _overrideMotion = serializedObject.FindProperty("overrideMotion");
            _motion = serializedObject.FindProperty("motion");
            _overrideTempo = serializedObject.FindProperty("overrideTempo");
            _tempo = serializedObject.FindProperty("tempo");
            _overrideAudience = serializedObject.FindProperty("overrideAudience");
            _audience = serializedObject.FindProperty("audience");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Timeline Fanlight Cue", EditorStyles.boldLabel);
            DrawColor();
            DrawMotion();
            DrawTempo();
            DrawAudience();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawColor()
        {
            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_overrideColor, new GUIContent("Override Color"));
            if (!_overrideColor.boolValue) return;

            using (new EditorGUI.IndentLevelScope())
            {
                var mode = _colorSettings.FindPropertyRelative("mode");
                EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
                EditorGUILayout.PropertyField(_colorSettings.FindPropertyRelative("primaryColor"), new GUIContent("Primary HDR Color"));

                if ((FanlightColorMode)mode.enumValueIndex == FanlightColorMode.Gradient)
                {
                    EditorGUILayout.PropertyField(_colorSettings.FindPropertyRelative("secondaryColor"), new GUIContent("Secondary HDR Color"));
                }
                else if ((FanlightColorMode)mode.enumValueIndex == FanlightColorMode.Random)
                {
                    EditorGUILayout.PropertyField(_colorSettings.FindPropertyRelative("paletteColors"), new GUIContent("HDR Palette"), true);
                }

                EditorGUILayout.PropertyField(_colorSettings.FindPropertyRelative("intensity"), new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(_colorSettings.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
            }
        }

        private void DrawMotion()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_overrideMotion, new GUIContent("Override Motion"));
            if (_overrideMotion.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_motion, new GUIContent("Settings"), true);
                }
            }
        }

        private void DrawTempo()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tempo", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_overrideTempo, new GUIContent("Override Tempo"));
            if (!_overrideTempo.boolValue) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.HelpBox("Timeline time is always the song-time source. This cue overrides only BPM, beats per bar, and timing offsets.", MessageType.None);
                EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("bpm"), new GUIContent("BPM"));
                EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("beatsPerBar"), new GUIContent("Beats Per Bar"));
                EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("offsetSeconds"), new GUIContent("Offset Seconds"));
                EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("latencyCompensationSeconds"), new GUIContent("Latency Compensation"));
            }
        }

        private void DrawAudience()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audience", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_overrideAudience, new GUIContent("Override Audience"));
            if (_overrideAudience.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_audience, new GUIContent("Settings"), true);
                }
            }
        }
    }

    [CustomTimelineEditor(typeof(FanlightTimelinePlayableAsset))]
    public sealed class FanlightTimelineClipEditor : ClipEditor
    {
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (clonedFrom == null)
            {
                clip.displayName = "Fanlight Cue";
            }
        }
    }
}
