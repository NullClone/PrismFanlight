using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightTimelineClipAsset), true)]
    [CanEditMultipleObjects]
    internal sealed class FanlightTimelineClipAssetInspector : UnityEditor.Editor
    {
        // Fields

        private const string ScriptPropertyName = "m_Script";
        private const string ValuePropertyName = "_value";


        // Methods

        public override void OnInspectorGUI()
        {
            FanlightPresetEditor.Draw(targets);

            serializedObject.Update();

            var property = serializedObject.GetIterator();
            var enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == ScriptPropertyName) continue;

                if (property.propertyPath == ValuePropertyName)
                {
                    DrawChildren(property);
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawChildren(SerializedProperty property)
        {
            var child = property.Copy();
            var end = child.GetEndProperty();
            var enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(child, true);
            }
        }
    }
}
