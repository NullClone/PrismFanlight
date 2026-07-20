using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightVisibilityClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightVisibilityState _value = FanlightTimelineDefaults.VisibilityState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
