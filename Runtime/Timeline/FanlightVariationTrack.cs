using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightVariationClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.25f, 0.75f, 0.55f)]
    public sealed class FanlightVariationTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Variation;
    }
}
