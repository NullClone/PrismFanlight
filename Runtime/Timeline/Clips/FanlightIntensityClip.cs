using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightIntensityClip : FanlightTimelineClipAsset
    {
        // Fields

        [SerializeField]
        private FanlightIntensityState _value = FanlightTimelineDefaults.IntensityState();


        // Properties

        internal override FanlightTimelineClipValue Value =>
            FanlightTimelineClipValue.From(_value.Validated());
    }
}
