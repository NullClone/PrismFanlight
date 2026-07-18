using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightRestClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.35f, 0.45f, 0.85f)]
    public sealed class FanlightRestTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Rest;
    }
}
