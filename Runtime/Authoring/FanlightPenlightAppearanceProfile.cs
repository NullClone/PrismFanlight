using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [CreateAssetMenu(fileName = "Penlight Appearance", menuName = "Prism Fanlight/Penlight Appearance Profile")]
    public sealed class FanlightPenlightAppearanceProfile : ScriptableObject
    {
        // Fields

        public const int CurrentAssignmentSchemaVersion = 1;
        public const int MaximumVariantCount = 4;

        [SerializeField, HideInInspector]
        private string _profileId = string.Empty;

        [SerializeField, Min(1)]
        private int _profileVersion = 1;

        [SerializeField, HideInInspector]
        private int _assignmentSchemaVersion = CurrentAssignmentSchemaVersion;

        [SerializeField]
        private uint _assignmentSeed = 1u;

        [SerializeField]
        private FanlightPenlightVariant[] _variants =
        {
            new(1u, null, true, 0f)
        };


        // Properties

        public string ProfileId => _profileId ?? string.Empty;

        public int ProfileVersion => _profileVersion;

        public int AssignmentSchemaVersion => _assignmentSchemaVersion;

        public uint AssignmentSeed => _assignmentSeed;

        public int VariantCount => _variants?.Length ?? 0;


        // Methods

        public FanlightPenlightVariant GetVariant(int index) => _variants[index];

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                error = "Appearance profile ID is missing.";
                return false;
            }

            if (_profileVersion <= 0)
            {
                error = "Appearance profile version must be greater than zero.";
                return false;
            }

            if (_assignmentSchemaVersion != CurrentAssignmentSchemaVersion)
            {
                error = $"Unsupported appearance assignment schema: {_assignmentSchemaVersion}.";
                return false;
            }

            if (VariantCount < 1 || VariantCount > MaximumVariantCount)
            {
                error = $"Appearance profile must contain between 1 and {MaximumVariantCount} variants.";
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
            AddString(ProfileId);
            AddInt(_profileVersion);
            AddInt(_assignmentSchemaVersion);
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

            void AddString(string value)
            {
                value ??= string.Empty;
                for (var i = 0; i < value.Length; i++)
                {
                    AddByte((byte)value[i]);
                    AddByte((byte)(value[i] >> 8));
                }
            }

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
            EnsureIdentity();
            _profileVersion = 1;
            _assignmentSchemaVersion = CurrentAssignmentSchemaVersion;
            _assignmentSeed = 1u;
            _variants = new[] { new FanlightPenlightVariant(1u, null, true, 0f) };
        }

        private void OnValidate()
        {
            EnsureIdentity();
            _profileVersion = Mathf.Max(1, _profileVersion);
        }

        private void EnsureIdentity()
        {
            if (string.IsNullOrWhiteSpace(_profileId)) _profileId = Guid.NewGuid().ToString("N");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }
}
