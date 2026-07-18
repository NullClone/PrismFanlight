using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightGestureClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.95f, 0.70f, 0.20f)]
    public sealed class FanlightGestureTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Gesture;
    }
}
