using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight
{
    public sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        public bool OverrideColor;
        public FanlightColorSettings Color;
        public bool OverrideMotion;
        public FanlightMotionSettings Motion;
        public bool OverrideTempo;
        public FanlightTempoSettings Tempo;
        public bool OverrideAudience;
        public FanlightAudienceSettings Audience;
    }
}
