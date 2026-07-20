using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuKernels
    {
        internal readonly int ClearIndirectArgs;
        internal readonly int CullBlocks;
        internal readonly int BuildVisibleInstances;
        internal readonly int GenerateVisibleAnimation;
        internal readonly int GenerateAllAnimation;
        internal readonly int GenerateVisibleFrameData;
        internal readonly int GenerateAllFrameData;


        internal FanlightGpuKernels(ComputeShader shader)
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
