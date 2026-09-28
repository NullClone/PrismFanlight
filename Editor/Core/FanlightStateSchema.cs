using System;
using System.Collections.Generic;
using System.Reflection;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightStateSchema
    {
        // Fields

        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly (FanlightStateKind Kind, Type State, Type Fields)[] Definitions =
        {
            (FanlightStateKind.Intent, typeof(FanlightIntentState), typeof(FanlightIntentFields)),
            (FanlightStateKind.Motion, typeof(FanlightMotionState), typeof(FanlightMotionFields)),
            (FanlightStateKind.Variation, typeof(FanlightVariationState), typeof(FanlightVariationFields)),
            (FanlightStateKind.Noise, typeof(FanlightNoiseState), typeof(FanlightNoiseFields)),
            (FanlightStateKind.Rest, typeof(FanlightRestState), typeof(FanlightRestFields)),
            (FanlightStateKind.AudienceBody, typeof(FanlightAudienceBodyState), typeof(FanlightAudienceBodyFields)),
            (FanlightStateKind.Direction, typeof(FanlightDirectionState), typeof(FanlightDirectionFields)),
            (FanlightStateKind.Color, typeof(FanlightColorState), typeof(FanlightColorFields)),
            (FanlightStateKind.Intensity, typeof(FanlightIntensityState), typeof(FanlightIntensityFields))
        };

        private static Dictionary<string, FanlightStateField>[] _fields;
        private static FanlightStateField[][] _patchableFields;
        private static int[] _allFields;


        // Methods

        internal static FanlightStateField GetField(FanlightStateKind kind, string propertyName)
        {
            EnsureBuilt();

            if (!_fields[(int)kind].TryGetValue(propertyName, out var field))
            {
                throw new InvalidOperationException($"{kind} State has no serialized field '{propertyName}'.");
            }

            return field;
        }

        internal static IReadOnlyList<FanlightStateField> GetPatchableFields(FanlightStateKind kind)
        {
            EnsureBuilt();
            return _patchableFields[(int)kind];
        }

        internal static int GetAllFields(FanlightStateKind kind)
        {
            EnsureBuilt();
            return _allFields[(int)kind];
        }

        internal static List<string> CollectErrors()
        {
            var errors = new List<string>();
            Build(errors, out _, out _, out _);
            return errors;
        }


        private static void EnsureBuilt()
        {
            if (_fields != null) return;

            var errors = new List<string>();
            Build(errors, out var fields, out var patchableFields, out var allFields);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Invalid Fanlight State schema:\n" + string.Join("\n", errors));
            }

            _patchableFields = patchableFields;
            _allFields = allFields;
            _fields = fields;
        }

        private static void Build(
            List<string> errors,
            out Dictionary<string, FanlightStateField>[] fields,
            out FanlightStateField[][] patchableFields,
            out int[] allFields)
        {
            var kinds = (FanlightStateKind[])Enum.GetValues(typeof(FanlightStateKind));
            fields = new Dictionary<string, FanlightStateField>[kinds.Length];
            patchableFields = new FanlightStateField[kinds.Length][];
            allFields = new int[kinds.Length];

            if (Definitions.Length != kinds.Length)
            {
                errors.Add($"Schema defines {Definitions.Length} State Kinds, but FanlightStateKind has {kinds.Length}.");
            }

            for (var i = 0; i < Definitions.Length; i++)
            {
                var definition = Definitions[i];
                var index = (int)definition.Kind;

                if (index < 0 || index >= kinds.Length || fields[index] != null)
                {
                    errors.Add($"{definition.Kind} is defined more than once or is out of range.");
                    continue;
                }

                fields[index] = BuildKind(definition.Kind, definition.State, definition.Fields, errors, out var patchable, out var all);
                patchableFields[index] = patchable;
                allFields[index] = all;
            }
        }

        private static Dictionary<string, FanlightStateField> BuildKind(
            FanlightStateKind kind,
            Type stateType,
            Type fieldsType,
            List<string> errors,
            out FanlightStateField[] patchableFields,
            out int allFields)
        {
            var fields = new Dictionary<string, FanlightStateField>();
            var patchable = new List<FanlightStateField>();
            var covered = 0;

            allFields = Enum.IsDefined(fieldsType, "All") ? Convert.ToInt32(Enum.Parse(fieldsType, "All")) : 0;
            if (allFields == 0)
            {
                errors.Add($"{fieldsType.Name} must define a non-zero All value.");
            }

            foreach (var member in stateType.GetFields(InstanceFields))
            {
                if (!IsSerialized(member)) continue;

                var attribute = member.GetCustomAttribute<FanlightFieldAttribute>();
                if (attribute == null)
                {
                    errors.Add($"{stateType.Name}.{member.Name} has no [FanlightField].");
                    continue;
                }

                var bit = 0;
                if (attribute.Field != null)
                {
                    if (attribute.Field.GetType() != fieldsType)
                    {
                        errors.Add($"{stateType.Name}.{member.Name} uses {attribute.Field.GetType().Name}, but {kind} requires {fieldsType.Name}.");
                        continue;
                    }

                    bit = Convert.ToInt32(attribute.Field);
                    if (bit == 0 || (bit & (bit - 1)) != 0 || (bit & ~allFields) != 0)
                    {
                        errors.Add($"{stateType.Name}.{member.Name} must use a single bit of {fieldsType.Name}.All.");
                        continue;
                    }

                    covered |= bit;
                }

                var field = new FanlightStateField(member.Name, bit, attribute.Label);
                fields.Add(member.Name, field);
                if (field.IsPatchable) patchable.Add(field);
            }

            if (covered != allFields)
            {
                errors.Add($"{fieldsType.Name} {Enum.ToObject(fieldsType, allFields & ~covered)} is not assigned to any field of {stateType.Name}.");
            }

            patchableFields = patchable.ToArray();
            return fields;
        }

        private static bool IsSerialized(FieldInfo member)
        {
            return !member.IsNotSerialized && (member.IsPublic || member.IsDefined(typeof(SerializeField), false));
        }
    }
}
