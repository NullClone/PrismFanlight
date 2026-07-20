using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightGestureClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.95f, 0.70f, 0.20f)]
    public sealed class FanlightGestureTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightGestureFields _fields = FanlightGestureFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Gesture;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
