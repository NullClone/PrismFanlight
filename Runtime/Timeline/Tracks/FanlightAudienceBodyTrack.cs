using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightAudienceBodyClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.55f, 0.35f, 0.85f)]
    public sealed class FanlightAudienceBodyTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightAudienceBodyFields _fields = FanlightAudienceBodyFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.AudienceBody;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
