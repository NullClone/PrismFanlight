using System;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    internal struct FanlightBakedBlockData
    {
        internal Vector3 localCenter;
        internal float radius;
        internal int startIndex;
        internal int count;
        internal Vector2 effectCoordinate;


        internal FanlightBakedBlockData(
            Vector3 localCenter,
            float radius,
            int startIndex,
            int count,
            Vector2 effectCoordinate)
        {
            this.localCenter = localCenter;
            this.radius = radius;
            this.startIndex = startIndex;
            this.count = count;
            this.effectCoordinate = effectCoordinate;
        }
    }
}
