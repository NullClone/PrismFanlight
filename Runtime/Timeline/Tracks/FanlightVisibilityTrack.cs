using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightVisibilityClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.70f, 0.70f, 0.70f)]
    public sealed class FanlightVisibilityTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightVisibilityFields _fields = FanlightVisibilityFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Visibility;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
