using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightAudienceBodyClip : FanlightTimelineClipAsset
    {
        [SerializeField] private FanlightAudienceBodyPatch _patch = FanlightTimelineDefaults.AudienceBodyPatch();

        internal override FanlightShowPatch Patch => new(default, default, default, default, default, default, _patch, default, default, default);
    }
}
