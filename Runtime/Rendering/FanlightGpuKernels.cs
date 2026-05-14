using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuKernels
    {
        public readonly int ClearIndirectArgs;
        public readonly int CullBlocks;
        public readonly int GenerateVisibleInstances;

        public FanlightGpuKernels(ComputeShader shader)
        {
            ClearIndirectArgs = shader.FindKernel("ClearIndirectArgs");
            CullBlocks = shader.FindKernel("CullBlocks");
            GenerateVisibleInstances = shader.FindKernel("GenerateVisibleInstances");
        }
    }
}
