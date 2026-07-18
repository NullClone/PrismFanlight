using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPaletteClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightPalettePatch _patch = FanlightTimelineDefaults.PalettePatch();

        internal override FanlightShowPatch Patch => new(default, default, default, default, default, default, default, default, _patch, default);
    }
}
