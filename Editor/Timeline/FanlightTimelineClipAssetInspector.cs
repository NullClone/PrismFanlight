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

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
