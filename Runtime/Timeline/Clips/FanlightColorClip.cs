using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightColorClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightColorState _value = FanlightTimelineDefaults.ColorState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value.Validated());
    }
}
