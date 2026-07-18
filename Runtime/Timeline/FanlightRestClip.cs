using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightRestClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightRestPatch _patch = FanlightTimelineDefaults.RestPatch();

        internal override FanlightShowPatch Patch => new(default, default, default, default, default, _patch, default, default, default, default);
    }
}
