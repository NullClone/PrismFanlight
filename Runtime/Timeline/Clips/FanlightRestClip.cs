using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightRestClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightRestState _value = FanlightTimelineDefaults.RestState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
