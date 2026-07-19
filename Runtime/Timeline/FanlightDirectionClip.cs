using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightDirectionClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightDirectionState _value = FanlightTimelineDefaults.DirectionState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
