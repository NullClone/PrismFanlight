using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal static class FanlightShaderIds
    {
        public static readonly int Seats = Shader.PropertyToID("_Seats");
        public static readonly int Blocks = Shader.PropertyToID("_Blocks");
        public static readonly int BlockVisibility = Shader.PropertyToID("_BlockVisibility");
        public static readonly int VisibleIndices = Shader.PropertyToID("_VisibleIndices");
        public static readonly int DrawArgs = Shader.PropertyToID("_DrawArgs");
        public static readonly int Matrices = Shader.PropertyToID("_FanlightMatrices");
        public static readonly int Colors = Shader.PropertyToID("_FanlightColors");
        public static readonly int BodyParts = Shader.PropertyToID("_BodyParts");
        public static readonly int BodyArgs = Shader.PropertyToID("_BodyArgs");

        public static readonly int InstanceCount = Shader.PropertyToID("_InstanceCount");
        public static readonly int BlockCountValue = Shader.PropertyToID("_BlockCountValue");
        public static readonly int LocalToWorld = Shader.PropertyToID("_LocalToWorld");
        public static readonly int Time = Shader.PropertyToID("_FanlightTime");
        public static readonly int Beat = Shader.PropertyToID("_FanlightBeat");
        public static readonly int Tempo = Shader.PropertyToID("_FanlightTempo");
        public static readonly int FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
        public static readonly int CullingScale = Shader.PropertyToID("_CullingScale");
        public static readonly int EnableCulling = Shader.PropertyToID("_EnableCulling");

        public static readonly int SeatPitch = Shader.PropertyToID("_SeatPitch");
        public static readonly int BlockCount = Shader.PropertyToID("_BlockCount");
        public static readonly int MotionTiming = Shader.PropertyToID("_MotionTiming");
        public static readonly int MotionSwing = Shader.PropertyToID("_MotionSwing");
        public static readonly int MotionShape = Shader.PropertyToID("_MotionShape");
        public static readonly int SwingMode = Shader.PropertyToID("_SwingMode");
        public static readonly int SwingWrist = Shader.PropertyToID("_SwingWrist");
        public static readonly int SwingAxis = Shader.PropertyToID("_SwingAxis");
        public static readonly int SwingTargetPos = Shader.PropertyToID("_SwingTargetPos");
        public static readonly int WorldToLocal = Shader.PropertyToID("_WorldToLocal");
        public static readonly int MotionVariation = Shader.PropertyToID("_MotionVariation");
        public static readonly int MotionNoise = Shader.PropertyToID("_MotionNoise");
        public static readonly int MotionHuman = Shader.PropertyToID("_MotionHuman");
        public static readonly int MotionRest = Shader.PropertyToID("_MotionRest");
        public static readonly int MotionRestTiming = Shader.PropertyToID("_MotionRestTiming");
        public static readonly int MotionBeat = Shader.PropertyToID("_MotionBeat");
        public static readonly int MotionBeatSpread = Shader.PropertyToID("_MotionBeatSpread");
        public static readonly int GripPivotY = Shader.PropertyToID("_GripPivotY");

        public static readonly int BodyShape = Shader.PropertyToID("_BodyShape");
        public static readonly int BodyArm = Shader.PropertyToID("_BodyArm");
        public static readonly int BodyReach = Shader.PropertyToID("_BodyReach");
        public static readonly int HandBaseHeight = Shader.PropertyToID("_HandBaseHeight");

        public static readonly int ColorMode = Shader.PropertyToID("_ColorMode");
        public static readonly int ColorSource = Shader.PropertyToID("_FanlightColorSource");
        public static readonly int GlobalColor = Shader.PropertyToID("_FanlightGlobalColor");
        public static readonly int GlobalIntensity = Shader.PropertyToID("_FanlightGlobalIntensity");
        public static readonly int PrimaryColor = Shader.PropertyToID("_PrimaryColor");
        public static readonly int SecondaryColor = Shader.PropertyToID("_SecondaryColor");
        public static readonly int Brightness = Shader.PropertyToID("_Brightness");
        public static readonly int PaletteColors = Shader.PropertyToID("_PaletteColors");
        public static readonly int PaletteColorCount = Shader.PropertyToID("_PaletteColorCount");
    }
}
