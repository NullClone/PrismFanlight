using System;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightBakedBlockData
    {
        public Vector3 localCenter;
        public float radius;
        public int startIndex;
        public int count;

        
        public FanlightBakedBlockData(Vector3 localCenter, float radius, int startIndex, int count)
        {
            this.localCenter = localCenter;
            this.radius = radius;
            this.startIndex = startIndex;
            this.count = count;
        }
    }
}
