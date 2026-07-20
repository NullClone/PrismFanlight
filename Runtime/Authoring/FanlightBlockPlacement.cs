using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal struct FanlightBlockPlacement : IEquatable<FanlightBlockPlacement>
    {
        // Fields

        [SerializeField]
        internal Vector3 position;

        [SerializeField]
        internal Vector3 eulerRotation;


        // Properties

        internal static FanlightBlockPlacement Identity => new()
        {
            position = Vector3.zero,
            eulerRotation = Vector3.zero
        };

        internal Quaternion Rotation => Quaternion.Euler(eulerRotation);


        // Methods

        public bool Equals(FanlightBlockPlacement other)
            => position.Equals(other.position) && eulerRotation.Equals(other.eulerRotation);
    }
}
