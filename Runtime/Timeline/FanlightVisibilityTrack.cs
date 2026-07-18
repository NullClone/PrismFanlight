using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightVisibilityClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.70f, 0.70f, 0.70f)]
    public sealed class FanlightVisibilityTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Visibility;
    }
}
