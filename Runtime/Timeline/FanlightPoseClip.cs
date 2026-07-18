using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPoseClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightPosePatch _patch = FanlightTimelineDefaults.PosePatch();

        internal override FanlightShowPatch Patch => new(default, default, _patch, default, default, default, default, default, default, default);
    }
}
