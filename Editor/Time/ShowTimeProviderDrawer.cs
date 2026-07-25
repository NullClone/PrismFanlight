using System;
using System.Linq;
using PrismFanlight.Time;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomPropertyDrawer(typeof(IShowTimeProvider), true)]
    public class ShowTimeProviderDrawer : PropertyDrawer
    {
        private static Type[] _types;
        private static string[] _typeNames;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeTypes();

            if (property.managedReferenceValue == null && _types.Length > 0)
            {
                property.managedReferenceValue = Activator.CreateInstance(typeof(UnityTimeProvider));
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginProperty(position, label, property);

            var popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            popupRect = EditorGUI.PrefixLabel(popupRect, label);

            var currentManagedObject = property.managedReferenceValue;
            int currentTypeIndex = 0;

            if (currentManagedObject != null)
            {
                var type = currentManagedObject.GetType();
                currentTypeIndex = Array.IndexOf(_types, type);
                if (currentTypeIndex == -1) currentTypeIndex = 0;
            }

            int selectedIndex = EditorGUI.Popup(popupRect, currentTypeIndex, _typeNames);

            if (selectedIndex != currentTypeIndex && selectedIndex >= 0 && selectedIndex < _types.Length)
            {
                property.managedReferenceValue = Activator.CreateInstance(_types[selectedIndex]);
            }

            if (property.managedReferenceValue != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var copy = property.Copy();
                    var end = copy.GetEndProperty();

                    bool enterChildren = true;
                    float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
                    {
                        enterChildren = false;

                        float height = EditorGUI.GetPropertyHeight(copy, true);
                        var propRect = new Rect(position.x, position.y + yOffset, position.width, height);

                        EditorGUI.PropertyField(propRect, copy, true);

                        yOffset += height + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitializeTypes();

            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                var copy = property.Copy();
                var end = copy.GetEndProperty();

                bool enterChildren = true;

                while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
                {
                    enterChildren = false;
                    height += EditorGUI.GetPropertyHeight(copy, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }


        private static void InitializeTypes()
        {
            if (_types != null) return;

            _types = TypeCache.GetTypesDerivedFrom<IShowTimeProvider>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .ToArray();

            _typeNames = new string[_types.Length];

            for (int i = 0; i < _types.Length; i++)
            {
                _typeNames[i] = _types[i].Name;
            }
        }
    }
}
