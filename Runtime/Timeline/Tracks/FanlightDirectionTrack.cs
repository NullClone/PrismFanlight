using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightDirectionClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.85f, 0.30f, 0.65f)]
    public sealed class FanlightDirectionTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightDirectionFields _fields = FanlightDirectionFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Direction;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
