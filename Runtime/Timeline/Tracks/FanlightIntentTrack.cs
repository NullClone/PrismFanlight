using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightIntentClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.95f, 0.45f, 0.20f)]
    public sealed class FanlightIntentTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightIntentFields _fields = FanlightIntentFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Intent;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
