using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightNoiseClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightNoiseState _value = FanlightTimelineDefaults.NoiseState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
