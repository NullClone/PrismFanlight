using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightPenlightRuntimeAppearance
    {
        private FanlightPenlightRuntimeAppearance(
            string profileId,
            int profileVersion,
            int assignmentSchemaVersion,
            uint assignmentSeed,
            ulong contentHash,
            Mesh[] meshes,
            uint[] stableVariantIds,
            float[] gripPivotYs,
            Bounds gripLocalBounds,
            bool isLegacy)
        {
            ProfileId = profileId ?? string.Empty;
            ProfileVersion = profileVersion;
            AssignmentSchemaVersion = assignmentSchemaVersion;
            AssignmentSeed = assignmentSeed;
            ContentHash = contentHash;
            Meshes = meshes ?? Array.Empty<Mesh>();
            StableVariantIds = stableVariantIds ?? Array.Empty<uint>();
            GripPivotYs = gripPivotYs ?? Array.Empty<float>();
            GripLocalBounds = gripLocalBounds;
            IsLegacy = isLegacy;
        }

        public string ProfileId { get; }

        public int ProfileVersion { get; }

        public int AssignmentSchemaVersion { get; }

        public uint AssignmentSeed { get; }

        public ulong ContentHash { get; }

        public Mesh[] Meshes { get; }

        public uint[] StableVariantIds { get; }

        public float[] GripPivotYs { get; }

        public Bounds GripLocalBounds { get; }

        public bool IsLegacy { get; }

        public int VariantCount => Meshes.Length;

        public float BoundsRadius => GripLocalBounds.center.magnitude + GripLocalBounds.extents.magnitude;

        public float BoundsPadding => BoundsRadius + 4f;

        public static FanlightPenlightRuntimeAppearance CreateLegacy(Mesh mesh)
        {
            if (mesh == null) return null;
            var pivotY = mesh.bounds.min.y;
            var bounds = ToGripLocalBounds(mesh.bounds, pivotY);
            return new FanlightPenlightRuntimeAppearance(
                "appearance.legacy",
                1,
                FanlightPenlightAssignment.SchemaVersion,
                0u,
                unchecked((ulong)(uint)mesh.GetInstanceID()) | 1UL,
                new[] { mesh },
                new[] { 1u },
                new[] { pivotY },
                bounds,
                true);
        }

        public static FanlightPenlightRuntimeAppearance Create(FanlightPenlightAppearanceProfile profile)
        {
            if (profile == null || !profile.TryValidate(out _)) return null;

            var variants = new FanlightPenlightVariant[profile.VariantCount];
            for (var i = 0; i < variants.Length; i++) variants[i] = profile.GetVariant(i);
            Array.Sort(variants, (left, right) => left.StableVariantId.CompareTo(right.StableVariantId));

            var meshes = new Mesh[variants.Length];
            var ids = new uint[variants.Length];
            var pivots = new float[variants.Length];
            var bounds = default(Bounds);
            var hasBounds = false;

            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                meshes[i] = variant.Mesh;
                ids[i] = variant.StableVariantId;
                pivots[i] = variant.GripPivotY;
                var variantBounds = ToGripLocalBounds(variant.Mesh.bounds, pivots[i]);
                if (!hasBounds)
                {
                    bounds = variantBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(variantBounds.min);
                    bounds.Encapsulate(variantBounds.max);
                }
            }

            return new FanlightPenlightRuntimeAppearance(
                profile.ProfileId,
                profile.ProfileVersion,
                profile.AssignmentSchemaVersion,
                profile.AssignmentSeed,
                profile.GetRuntimeContentHash(),
                meshes,
                ids,
                pivots,
                bounds,
                false);
        }

        private static Bounds ToGripLocalBounds(Bounds meshBounds, float gripPivotY)
        {
            meshBounds.center -= new Vector3(0f, gripPivotY, 0f);
            return meshBounds;
        }
    }
}
