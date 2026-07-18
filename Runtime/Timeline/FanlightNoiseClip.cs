using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightNoiseClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightNoisePatch _patch = FanlightTimelineDefaults.NoisePatch();

        internal override FanlightShowPatch Patch => new(default, default, default, default, _patch, default, default, default, default, default);
    }
}
