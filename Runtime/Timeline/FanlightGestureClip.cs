using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightGestureClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightGesturePatch _patch = FanlightTimelineDefaults.GesturePatch();

        internal override FanlightShowPatch Patch => new(default, _patch, default, default, default, default, default, default, default, default);
    }
}
