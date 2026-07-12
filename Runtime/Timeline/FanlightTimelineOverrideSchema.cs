using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal enum FanlightTimelineSettingsGroup
    {
        Color,
        Motion,
        Tempo,
        Audience
    }

    internal static class FanlightTimelineOverrideSchema
    {
        private static readonly Dictionary<string, FanlightTimelineOverrideDescriptor> ByPath = new();
        private static readonly Dictionary<FanlightTimelineSettingsGroup, List<FanlightTimelineOverrideDescriptor>> ByGroup = new();


        static FanlightTimelineOverrideSchema()
        {
            AddGroup(FanlightTimelineSettingsGroup.Color, "color", typeof(FanlightColorSettings));
            AddGroup(FanlightTimelineSettingsGroup.Motion, "motion", typeof(FanlightMotionSettings));
            AddGroup(FanlightTimelineSettingsGroup.Tempo, "tempo", typeof(FanlightTempoSettings));
            AddGroup(FanlightTimelineSettingsGroup.Audience, "audience", typeof(FanlightAudienceSettings));
        }

        public static IReadOnlyList<FanlightTimelineOverrideDescriptor> GetGroup(FanlightTimelineSettingsGroup group) => ByGroup[group];

        public static bool TryGet(string path, out FanlightTimelineOverrideDescriptor descriptor) => ByPath.TryGetValue(path, out descriptor);

        public static IEnumerable<string> GetPaths(FanlightTimelineSettingsGroup group) => ByGroup[group].Select(descriptor => descriptor.Path);

        private static void AddGroup(FanlightTimelineSettingsGroup group, string rootPath, Type rootType)
        {
            var descriptors = new List<FanlightTimelineOverrideDescriptor>();

            Collect(group, rootPath, string.Empty, rootType, Array.Empty<FieldInfo>(), descriptors);

            descriptors.Sort((left, right) => string.CompareOrdinal(left.RelativePath, right.RelativePath));

            ByGroup.Add(group, descriptors);

            foreach (var descriptor in descriptors)
            {
                ByPath.Add(descriptor.Path, descriptor);
            }
        }

        private static void Collect(
            FanlightTimelineSettingsGroup group,
            string rootPath,
            string relativePath,
            Type type,
            FieldInfo[] parentFields,
            List<FanlightTimelineOverrideDescriptor> descriptors)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.IsDefined(typeof(FanlightTimelineIgnoreAttribute), false)) continue;

                var fieldPath = string.IsNullOrEmpty(relativePath)
                    ? field.Name
                    : $"{relativePath}.{field.Name}";
                var fields = Append(parentFields, field);

                if (IsLeaf(field.FieldType))
                {
                    var blend = field.GetCustomAttribute<FanlightTimelineBlendAttribute>()?.Mode ?? FanlightTimelineBlendMode.Auto;
                    descriptors.Add(new FanlightTimelineOverrideDescriptor(
                        group,
                        $"{rootPath}.{fieldPath}",
                        fieldPath,
                        GetDisplayGroup(relativePath),
                        Nicify(field.Name),
                        field.FieldType,
                        blend,
                        fields));
                    continue;
                }

                if (field.FieldType.IsValueType)
                {
                    Collect(group, rootPath, fieldPath, field.FieldType, fields, descriptors);
                }
            }
        }

        private static FieldInfo[] Append(FieldInfo[] fields, FieldInfo field)
        {
            var result = new FieldInfo[fields.Length + 1];
            Array.Copy(fields, result, fields.Length);
            result[^1] = field;
            return result;
        }

        private static bool IsLeaf(Type type)
        {
            return type.IsPrimitive
                   || type == typeof(Color)
                   || type == typeof(Vector2)
                   || type == typeof(Vector3)
                   || type.IsEnum
                   || type.IsArray;
        }

        private static string GetDisplayGroup(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "General";
            var separator = relativePath.IndexOf('.');
            var group = separator < 0 ? relativePath : relativePath[..separator];
            return Nicify(group);
        }

        private static string Nicify(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var result = new StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (i > 0 && char.IsUpper(current) && !char.IsUpper(value[i - 1])) result.Append(' ');
                result.Append(i == 0 ? char.ToUpperInvariant(current) : current);
            }

            return result.ToString();
        }
    }
}
