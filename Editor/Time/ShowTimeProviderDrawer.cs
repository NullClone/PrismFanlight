using System;
using System.Linq;
using PrismFanlight.Time;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomPropertyDrawer(typeof(IShowTimeProvider), true)]
    internal sealed class ShowTimeProviderDrawer : PropertyDrawer
    {
        private static Type[] _types;
        private static string[] _typeNames;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeTypes();

            EditorGUI.BeginProperty(position, label, property);

            var popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            popupRect = EditorGUI.PrefixLabel(popupRect, label);

            var currentManagedObject = property.managedReferenceValue;
            var currentTypeIndex = 0;

            if (currentManagedObject != null)
            {
                var type = currentManagedObject.GetType();
                var typeIndex = Array.IndexOf(_types, type);
                if (typeIndex >= 0) currentTypeIndex = typeIndex + 1;
            }

            var selectedIndex = EditorGUI.Popup(popupRect, currentTypeIndex, _typeNames);

            if (selectedIndex != currentTypeIndex)
            {
                property.managedReferenceValue = selectedIndex == 0
                    ? null
                    : CreateProvider(_types[selectedIndex - 1]);
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
                .Where(type => type.IsClass
                    && (type.IsPublic || type.IsNestedPublic)
                    && !type.IsAbstract
                    && !type.ContainsGenericParameters
                    && type.IsSerializable
                    && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                    && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            _typeNames = new string[_types.Length + 1];
            _typeNames[0] = "None";

            for (var i = 0; i < _types.Length; i++)
            {
                _typeNames[i + 1] = _types[i].Name;
            }
        }

        private static object CreateProvider(Type providerType)
        {
            try
            {
                return Activator.CreateInstance(providerType);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }
    }
}
