using System;
using System.Reflection;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineOverrideDescriptor
    {
        // Fields

        private const float DiscreteSwitchWeight = 0.5f;

        private readonly FieldInfo[] _fields;
        private readonly object[] _parents;


        // Methods

        public FanlightTimelineSettingsGroup Group { get; }

        public string Path { get; }

        public string RelativePath { get; }

        public string DisplayGroup { get; }

        public string DisplayName { get; }

        private Type ValueType { get; }

        private FanlightTimelineBlendMode BlendMode { get; }


        // Methods

        public FanlightTimelineOverrideDescriptor(
            FanlightTimelineSettingsGroup group,
            string path,
            string relativePath,
            string displayGroup,
            string displayName,
            Type valueType,
            FanlightTimelineBlendMode blendMode,
            FieldInfo[] fields)
        {
            Group = group;
            Path = path;
            RelativePath = relativePath;
            DisplayGroup = displayGroup;
            DisplayName = displayName;
            ValueType = valueType;
            BlendMode = blendMode;
            _fields = fields;
            _parents = new object[fields.Length];
        }

        public object GetValue(object root)
        {
            var current = root;
            foreach (var field in _fields)
            {
                current = field.GetValue(current);
            }

            return current;
        }

        public object SetValue(object root, object value)
        {
            var current = root;
            for (var i = 0; i < _fields.Length; i++)
            {
                _parents[i] = current;
                current = _fields[i].GetValue(current);
            }

            var child = value;
            for (var i = _fields.Length - 1; i >= 0; i--)
            {
                _fields[i].SetValue(_parents[i], child);
                child = _parents[i];
            }

            return child;
        }

        public object Blend(object from, object to, float weight)
        {
            var t = Mathf.Clamp01(weight);

            if (IsDiscrete() && t < DiscreteSwitchWeight) return from;
            if (IsDiscrete()) return to;

            if (ValueType == typeof(float))
            {
                return BlendMode == FanlightTimelineBlendMode.Angle
                    ? Mathf.LerpAngle((float)from, (float)to, t)
                    : Mathf.Lerp((float)from, (float)to, t);
            }

            if (ValueType == typeof(Color)) return Color.Lerp((Color)from, (Color)to, t);
            if (ValueType == typeof(Vector2)) return Vector2.Lerp((Vector2)from, (Vector2)to, t);
            if (ValueType == typeof(Vector3)) return Vector3.Lerp((Vector3)from, (Vector3)to, t);

            return to;
        }

        public bool IsDiscrete()
        {
            return BlendMode == FanlightTimelineBlendMode.Discrete
                   || (ValueType.IsPrimitive && ValueType != typeof(float))
                   || ValueType.IsEnum
                   || ValueType.IsArray;
        }
    }
}
