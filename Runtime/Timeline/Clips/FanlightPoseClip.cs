using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPoseClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightPoseState _value = FanlightTimelineDefaults.PoseState();

        internal override FanlightTimelineClipValue Value
        {
            get
            {
#if UNITY_EDITOR
                return FanlightTimelineClipValue.From(FanlightShowStateAuthoringValidator.Validate(_value));
#else
                return FanlightTimelineClipValue.From(_value);
#endif
            }
        }


        // Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            _value = FanlightShowStateAuthoringValidator.Validate(_value);
        }
#endif
    }
}
