using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightVisibilityClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightVisibilityPatch _patch = FanlightTimelineDefaults.VisibilityPatch();

        internal override FanlightShowPatch Patch => new(default, default, default, default, default, default, default, default, default, _patch);
    }
}
