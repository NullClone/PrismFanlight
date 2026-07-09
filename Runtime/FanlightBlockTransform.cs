using System;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightBlockTransform : IEquatable<FanlightBlockTransform>
    {
        public Vector3 position;
        public Vector3 eulerRotation;

        public static FanlightBlockTransform Identity => new()
        {
            position = Vector3.zero,
            eulerRotation = Vector3.zero
        };

        public Quaternion Rotation => Quaternion.Euler(eulerRotation);

        public bool Equals(FanlightBlockTransform other)
        {
            return position.Equals(other.position)
                   && eulerRotation.Equals(other.eulerRotation);
        }

        public override bool Equals(object obj)
        {
            return obj is FanlightBlockTransform other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (position.GetHashCode() * 397) ^ eulerRotation.GetHashCode();
            }
        }
    }
}
