using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightAudienceBodyClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.55f, 0.35f, 0.85f)]
    public sealed class FanlightAudienceBodyTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.AudienceBody;
    }
}
