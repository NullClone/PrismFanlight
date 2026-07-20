using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal static class FanlightShaderIds
    {
        internal static readonly int Seats = Shader.PropertyToID("_Seats");
        internal static readonly int Blocks = Shader.PropertyToID("_Blocks");
        internal static readonly int BlockVisibility = Shader.PropertyToID("_BlockVisibility");
        internal static readonly int VisibleIndices = Shader.PropertyToID("_VisibleIndices");
        internal static readonly int PenlightVisibleIndices = Shader.PropertyToID("_PenlightVisibleIndices");
        internal static readonly int PenlightVariantAssignments = Shader.PropertyToID("_PenlightVariantAssignments");
        internal static readonly int PenlightVariantOffsets = Shader.PropertyToID("_PenlightVariantOffsets");
        internal static readonly int AudienceVisibleIndices = Shader.PropertyToID("_AudienceVisibleIndices");
        internal static readonly int AudienceSlots = Shader.PropertyToID("_AudienceSlots");
        internal static readonly int PenlightArgs = Shader.PropertyToID("_PenlightArgs");
        internal static readonly int Matrices = Shader.PropertyToID("_FanlightMatrices");
        internal static readonly int ColorAssignments = Shader.PropertyToID("_FanlightColorAssignments");
        internal static readonly int Randoms = Shader.PropertyToID("_FanlightRandoms");
        internal static readonly int AudienceParts = Shader.PropertyToID("_AudienceParts");
        internal static readonly int AudienceArgs = Shader.PropertyToID("_AudienceArgs");

        internal static readonly int InstanceCount = Shader.PropertyToID("_InstanceCount");
        internal static readonly int PenlightVariantCount = Shader.PropertyToID("_PenlightVariantCount");
        internal static readonly int PenlightVariantGripPivotYs = Shader.PropertyToID("_PenlightVariantGripPivotYs");
        internal static readonly int VisibleIndexBase = Shader.PropertyToID("_VisibleIndexBase");
        internal static readonly int BlockCountValue = Shader.PropertyToID("_BlockCountValue");
        internal static readonly int LocalToWorld = Shader.PropertyToID("_LocalToWorld");
        internal static readonly int Time = Shader.PropertyToID("_FanlightTime");
        internal static readonly int Beat = Shader.PropertyToID("_FanlightBeat");
        internal static readonly int Tempo = Shader.PropertyToID("_FanlightTempo");
        internal static readonly int FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
        internal static readonly int CullingScale = Shader.PropertyToID("_CullingScale");
        internal static readonly int EnableCulling = Shader.PropertyToID("_EnableCulling");
        internal static readonly int EnableAudienceLod = Shader.PropertyToID("_EnableAudienceLod");
        internal static readonly int AudienceLod = Shader.PropertyToID("_AudienceLod");
        internal static readonly int LodCameraPos = Shader.PropertyToID("_LodCameraPos");

        internal static readonly int SeatPitch = Shader.PropertyToID("_SeatPitch");
        internal static readonly int BlockCount = Shader.PropertyToID("_BlockCount");
        internal static readonly int MotionTiming = Shader.PropertyToID("_MotionTiming");
        internal static readonly int MotionSwing = Shader.PropertyToID("_MotionSwing");
        internal static readonly int MotionShape = Shader.PropertyToID("_MotionShape");
        internal static readonly int SwingMode = Shader.PropertyToID("_SwingMode");
        internal static readonly int SwingWrist = Shader.PropertyToID("_SwingWrist");
        internal static readonly int SwingAxis = Shader.PropertyToID("_SwingAxis");
        internal static readonly int SwingTargetPos = Shader.PropertyToID("_SwingTargetPos");
        internal static readonly int WorldToLocal = Shader.PropertyToID("_WorldToLocal");
        internal static readonly int MotionVariation = Shader.PropertyToID("_MotionVariation");
        internal static readonly int MotionNoise = Shader.PropertyToID("_MotionNoise");
        internal static readonly int MotionHuman = Shader.PropertyToID("_MotionHuman");
        internal static readonly int MotionRest = Shader.PropertyToID("_MotionRest");
        internal static readonly int MotionRestTiming = Shader.PropertyToID("_MotionRestTiming");
        internal static readonly int MotionBeat = Shader.PropertyToID("_MotionBeat");
        internal static readonly int MotionBeatSpread = Shader.PropertyToID("_MotionBeatSpread");
        internal static readonly int GripPivotY = Shader.PropertyToID("_GripPivotY");

        internal static readonly int AudienceShape = Shader.PropertyToID("_AudienceShape");
        internal static readonly int AudienceArm = Shader.PropertyToID("_AudienceArm");
        internal static readonly int HandZone = Shader.PropertyToID("_HandZone");
        internal static readonly int AudienceUpperBody = Shader.PropertyToID("_AudienceUpperBody");
        internal static readonly int AudienceMotionBody = Shader.PropertyToID("_AudienceMotionBody");

        internal static readonly int GlobalIntensity = Shader.PropertyToID("_FanlightGlobalIntensity");
        internal static readonly int RandomIntensity = Shader.PropertyToID("_FanlightRandomIntensity");
        internal static readonly int PaletteColors = Shader.PropertyToID("_PaletteColors");
    }
}
