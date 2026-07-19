using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPaletteClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightPaletteState _value = FanlightTimelineDefaults.PaletteState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
