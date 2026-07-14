using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuKernels
    {
        public readonly int ClearIndirectArgs;
        public readonly int CullBlocks;
        public readonly int BuildVisibleInstances;
        public readonly int GenerateVisibleAnimation;
        public readonly int GenerateAllAnimation;
        public readonly int GenerateVisibleFrameData;
        public readonly int GenerateAllFrameData;

        public FanlightGpuKernels(ComputeShader shader)
        {
            ClearIndirectArgs = shader.FindKernel("ClearIndirectArgs");
            CullBlocks = shader.FindKernel("CullBlocks");
            BuildVisibleInstances = shader.FindKernel("BuildVisibleInstances");
            GenerateVisibleAnimation = shader.FindKernel("GenerateVisibleAnimation");
            GenerateAllAnimation = shader.FindKernel("GenerateAllAnimation");
            GenerateVisibleFrameData = shader.FindKernel("GenerateVisibleFrameData");
            GenerateAllFrameData = shader.FindKernel("GenerateAllFrameData");
        }
    }
}
