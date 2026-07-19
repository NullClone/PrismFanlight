using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightRestClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.35f, 0.45f, 0.85f)]
    public sealed class FanlightRestTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightRestFields _fields = FanlightRestFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Rest;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
