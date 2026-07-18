using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightNoiseClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.20f, 0.65f, 0.85f)]
    public sealed class FanlightNoiseTrack : FanlightTimelineTrackAsset
    {
        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Noise;
    }
}
