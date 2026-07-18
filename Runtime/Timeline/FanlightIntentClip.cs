using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightIntentClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightIntentPatch _patch = FanlightTimelineDefaults.IntentPatch();

        internal override FanlightShowPatch Patch => new(_patch, default, default, default, default, default, default, default, default, default);
    }
}
