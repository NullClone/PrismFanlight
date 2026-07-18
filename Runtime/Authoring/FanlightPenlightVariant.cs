using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    public struct FanlightPenlightVariant
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

        public uint StableVariantId => _stableVariantId;

        public Mesh Mesh => _mesh;

        public bool UseMeshBoundsMinimumAsGrip => _useMeshBoundsMinimumAsGrip;

        public float GripPivotY => _mesh != null && _useMeshBoundsMinimumAsGrip
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
