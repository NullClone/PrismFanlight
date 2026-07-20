using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPoseClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightPoseState _value = FanlightTimelineDefaults.PoseState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
