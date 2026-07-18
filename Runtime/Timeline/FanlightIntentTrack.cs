using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightIntentClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.95f, 0.45f, 0.20f)]
    public sealed class FanlightIntentTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Intent;
    }
}
