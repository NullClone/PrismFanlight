using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [CreateAssetMenu(fileName = "New Penlight Asset", menuName = "Prism Fanlight/Penlight Asset")]
    public sealed class FanlightPenlightAsset : ScriptableObject
    {
        // Fields

        internal const int MaximumVariantCount = 4;

        [SerializeField]
        private uint _assignmentSeed = 1u;

        [SerializeField]
        private FanlightPenlightVariant[] _variants =
        {
            new(1u, null, true, 0f)
        };


        // Properties

        internal uint AssignmentSeed => _assignmentSeed;

        internal int VariantCount => _variants?.Length ?? 0;


        // Methods

        internal FanlightPenlightVariant GetVariant(int index) => _variants[index];

        internal bool TryValidate(out string error)
        {
            if (VariantCount < 1 || VariantCount > MaximumVariantCount)
            {
                error = $"Penlight Asset must contain between 1 and {MaximumVariantCount} variants.";
                return false;
            }

            for (var i = 0; i < VariantCount; i++)
            {
                var variant = _variants[i];
                if (variant.StableVariantId == 0u)
                {
                    error = $"Variant {i} has a zero stable ID.";
                    return false;
                }

                for (var previous = 0; previous < i; previous++)
                {
                    if (_variants[previous].StableVariantId != variant.StableVariantId) continue;
                    error = $"Variant {i} has a duplicate stable ID.";
                    return false;
                }

                if (variant.Mesh == null || variant.Mesh.subMeshCount < 1 || variant.Mesh.GetIndexCount(0) == 0u)
                {
                    error = $"Variant {variant.StableVariantId} requires a non-empty mesh with submesh 0.";
                    return false;
                }

                if (!IsFinite(variant.GripPivotY)
                    || !IsFinite(variant.Mesh.bounds.center)
                    || !IsFinite(variant.Mesh.bounds.extents))
                {
                    error = $"Variant {variant.StableVariantId} has a non-finite grip pivot.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        internal ulong GetRuntimeContentHash()
        {
            var hash = 14695981039346656037UL;
            AddUInt(_assignmentSeed);
            AddInt(VariantCount);
            for (var i = 0; i < VariantCount; i++)
            {
                var variant = _variants[i];
                AddUInt(variant.StableVariantId);
                AddInt(variant.Mesh != null ? variant.Mesh.GetInstanceID() : 0);
                if (variant.Mesh != null && variant.Mesh.subMeshCount > 0)
                {
                    AddUInt(variant.Mesh.GetIndexCount(0));
                    AddUInt(variant.Mesh.GetIndexStart(0));
                    AddUInt(variant.Mesh.GetBaseVertex(0));
                    AddVector3(variant.Mesh.bounds.center);
                    AddVector3(variant.Mesh.bounds.extents);
                }

                AddFloat(variant.GripPivotY);
            }

            return hash == 0UL ? 1UL : hash;

            void AddInt(int value) => AddUInt(unchecked((uint)value));

            void AddUInt(uint value)
            {
                for (var i = 0; i < 4; i++) AddByte((byte)(value >> (i * 8)));
            }

            void AddFloat(float value) => AddInt(BitConverter.SingleToInt32Bits(value));

            void AddVector3(Vector3 value)
            {
                AddFloat(value.x);
                AddFloat(value.y);
                AddFloat(value.z);
            }

            void AddByte(byte value)
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }
        }

        private void Reset()
        {
            _assignmentSeed = 1u;
            _variants = new[] { new FanlightPenlightVariant(1u, null, true, 0f) };
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }
}
