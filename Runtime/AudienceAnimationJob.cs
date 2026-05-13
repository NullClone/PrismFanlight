using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PrismFanlight
{
    [BurstCompile]
    public struct AudienceAnimationJob : IJobParallelFor
    {
        public Audience config;
        public FanlightMotionSettings motion;
        public FanlightColorSettings color;
        public Matrix4x4 xform;
        public float time;

        public NativeSlice<Matrix4x4> matrices;
        public NativeSlice<Color> colors;

        public void Execute(int i)
        {
            var (block, seat) = config.GetCoordinatesFromIndex(i);
            var pos = config.GetPositionOnPlane(block, seat);
            var seed = (uint)i * 2u + 123u;
            matrices[i] = motion.GetMatrix(config, pos, xform, time, seed++);
            colors[i] = color.GetColor(config, block, pos, time, seed++);
        }
    }
}
