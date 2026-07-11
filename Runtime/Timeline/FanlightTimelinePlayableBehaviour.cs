using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        public FanlightColorSettings Color;
        public FanlightMotionSettings Motion;
        public FanlightTempoSettings Tempo;
        public FanlightAudienceSettings Audience;
        public FanlightTimelineOverrideSelection Overrides;
    }
}
