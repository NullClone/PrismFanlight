using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal struct FanlightPenlightVariant
    {
        // Fields

        [SerializeField, Min(1)]
        private uint _stableVariantId;

        [SerializeField]
        private Mesh _mesh;

        [SerializeField]
        private bool _useMeshBoundsMinimumAsGrip;

        [SerializeField]
        private float _gripPivotY;


        // Properties

        internal uint StableVariantId => _stableVariantId;

        internal Mesh Mesh => _mesh;

        internal bool UseMeshBoundsMinimumAsGrip => _useMeshBoundsMinimumAsGrip;

        internal float GripPivotY => _mesh != null && _useMeshBoundsMinimumAsGrip
            ? _mesh.bounds.min.y
            : _gripPivotY;


        // Methods

        internal FanlightPenlightVariant(uint stableVariantId, Mesh mesh, bool useMeshBoundsMinimumAsGrip, float gripPivotY)
        {
            _stableVariantId = stableVariantId;
            _mesh = mesh;
            _useMeshBoundsMinimumAsGrip = useMeshBoundsMinimumAsGrip;
            _gripPivotY = gripPivotY;
        }
    }
}
