using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightDirectionClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.85f, 0.30f, 0.65f)]
    public sealed class FanlightDirectionTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Direction;
    }
}
