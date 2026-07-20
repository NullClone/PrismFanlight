using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightGestureClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightGestureState _value = FanlightTimelineDefaults.GestureState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
