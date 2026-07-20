using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightPenlightRuntimeAppearance
    {
        // Properties

        internal uint AssignmentSeed { get; }

        internal ulong ContentHash { get; }

        internal Mesh[] Meshes { get; }

        internal uint[] StableVariantIds { get; }

        internal float[] GripPivotYs { get; }

        internal Bounds GripLocalBounds { get; }

        internal int VariantCount => Meshes.Length;

        internal float BoundsRadius => GripLocalBounds.center.magnitude + GripLocalBounds.extents.magnitude;

        internal float BoundsPadding => BoundsRadius + 4f;


        // Methods

        private FanlightPenlightRuntimeAppearance(
            uint assignmentSeed,
            ulong contentHash,
            Mesh[] meshes,
            uint[] stableVariantIds,
            float[] gripPivotYs,
            Bounds gripLocalBounds)
        {
            AssignmentSeed = assignmentSeed;
            ContentHash = contentHash;
            Meshes = meshes ?? Array.Empty<Mesh>();
            StableVariantIds = stableVariantIds ?? Array.Empty<uint>();
            GripPivotYs = gripPivotYs ?? Array.Empty<float>();
            GripLocalBounds = gripLocalBounds;
        }

        internal static FanlightPenlightRuntimeAppearance Create(FanlightPenlightAppearanceProfile profile)
        {
            if (profile == null || !profile.TryValidate(out _)) return null;

            var variants = new FanlightPenlightVariant[profile.VariantCount];

            for (var i = 0; i < variants.Length; i++)
            {
                variants[i] = profile.GetVariant(i);
            }

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
                profile.AssignmentSeed,
                profile.GetRuntimeContentHash(),
                meshes,
                ids,
                pivots,
                bounds);
        }

        private static Bounds ToGripLocalBounds(Bounds meshBounds, float gripPivotY)
        {
            meshBounds.center -= new Vector3(0f, gripPivotY, 0f);
            return meshBounds;
        }
    }
}
