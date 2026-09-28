using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightIntensityClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightIntensityState _value = FanlightTimelineDefaults.IntensityState();

        internal override FanlightStateKind StateKind => FanlightStateKind.Intensity;

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value.Validated());
    }
}
