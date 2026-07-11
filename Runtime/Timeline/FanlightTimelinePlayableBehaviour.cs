using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        public FanlightTimelinePlayableAsset Asset;
        public FanlightColorSettings Color;
        public FanlightMotionSettings Motion;
        public FanlightTempoSettings Tempo;
        public FanlightAudienceSettings Audience;
        public FanlightTimelineOverrideSelection Overrides;


        public FanlightColorSettings GetColor() => Asset != null ? Asset.GetColorSettings() : Color;

        public FanlightMotionSettings GetMotion() => Asset != null ? Asset.GetMotionSettings() : Motion;

        public FanlightTempoSettings GetTempo() => Asset != null ? Asset.GetTempoSettings() : Tempo;

        public FanlightAudienceSettings GetAudience() => Asset != null ? Asset.GetAudienceSettings() : Audience;

        public FanlightTimelineOverrideSelection GetOverrides() => Asset != null ? Asset.GetTimelineOverrides() : Overrides;
    }
}
