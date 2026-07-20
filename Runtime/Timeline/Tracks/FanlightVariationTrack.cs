using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightVariationClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.25f, 0.75f, 0.55f)]
    public sealed class FanlightVariationTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightVariationFields _fields = FanlightVariationFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Variation;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
