using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightVariationClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightVariationPatch _patch = FanlightTimelineDefaults.VariationPatch();

        internal override FanlightShowPatch Patch => new(default, default, default, _patch, default, default, default, default, default, default);
    }
}
