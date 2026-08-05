using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuKernels
    {
        // Fields

        internal readonly int ClearIndirectArgs;
        internal readonly int CullBlocks;
        internal readonly int BuildVisibleInstances;
        internal readonly int GenerateAllAnimation;
        internal readonly int GenerateAllFrameData;
        internal readonly int ResolveSeatChroma;
        internal readonly int ResolveSeatMask;


        // Methods

        internal FanlightGpuKernels(ComputeShader shader)
        {
            ClearIndirectArgs = shader.FindKernel("ClearIndirectArgs");
            CullBlocks = shader.FindKernel("CullBlocks");
            BuildVisibleInstances = shader.FindKernel("BuildVisibleInstances");
            GenerateAllAnimation = shader.FindKernel("GenerateAllAnimation");
            GenerateAllFrameData = shader.FindKernel("GenerateAllFrameData");
            ResolveSeatChroma = shader.FindKernel("ResolveSeatChroma");
            ResolveSeatMask = shader.FindKernel("ResolveSeatMask");
        }
    }
}
