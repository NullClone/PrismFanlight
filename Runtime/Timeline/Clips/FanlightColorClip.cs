using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightColorClip : FanlightTimelineClipAsset
    {
        // Fields

        [SerializeField]
        private FanlightColorState _value = FanlightTimelineDefaults.ColorState();


        // Properties

        internal override FanlightTimelineClipValue Value =>
            FanlightTimelineClipValue.From(_value.Validated());
    }
}
