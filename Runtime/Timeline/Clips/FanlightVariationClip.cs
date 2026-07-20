using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightVariationClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightVariationState _value = FanlightTimelineDefaults.VariationState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
