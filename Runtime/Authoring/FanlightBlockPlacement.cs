using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    public struct FanlightBlockPlacement : IEquatable<FanlightBlockPlacement>
    {
        // Fields

        public Vector3 position;
        public Vector3 eulerRotation;


        // Properties

        public static FanlightBlockPlacement Identity => new()
        {
            position = Vector3.zero,
            eulerRotation = Vector3.zero
        };

        public Quaternion Rotation => Quaternion.Euler(eulerRotation);


        // Methods

        public bool Equals(FanlightBlockPlacement other)
        {
            return position.Equals(other.position) && eulerRotation.Equals(other.eulerRotation);
        }
    }
}
