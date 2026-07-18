using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightPoseClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.60f, 0.80f, 0.25f)]
    public sealed class FanlightPoseTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Pose;
    }
}
