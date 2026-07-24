using PrismFanlight.Core;
using PrismFanlight.Time;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(ManualTimeProvider))]
    internal sealed class ManualTimeProviderEditor : UnityEditor.Editor
    {
        // Fields

        private ManualTimeProvider _instance;
        private SerializedProperty _iterator;
        private SerializedProperty _seconds;
        private SerializedProperty _rate;


        // Methods

        private void OnEnable()
        {
            _instance = target as ManualTimeProvider;

            _iterator = serializedObject.FindProperty("m_Script");
            _seconds = serializedObject.FindProperty(nameof(_seconds));
            _rate = serializedObject.FindProperty(nameof(_rate));
        }

        public override void OnInspectorGUI()
        {
            if (_instance == null) return;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_iterator);
            }

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            var seconds = EditorGUILayout.DoubleField(new GUIContent("Seconds"), _seconds.doubleValue);
            var secondsChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            var rate = EditorGUILayout.DoubleField(new GUIContent("Rate"), _rate.doubleValue);
            var rateChanged = EditorGUI.EndChangeCheck();

            if (secondsChanged)
            {
                Undo.RecordObject(_instance, "Set Manual Time");

                _instance.SetTime(seconds, rate, FanlightTimeDiscontinuity.Seek);

                EditorUtility.SetDirty(_instance);

                serializedObject.Update();

                return;
            }

            if (rateChanged)
            {
                _rate.doubleValue = rate;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
