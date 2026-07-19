using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightPoseClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.60f, 0.80f, 0.25f)]
    public sealed class FanlightPoseTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightPoseFields _fields = FanlightPoseFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Pose;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
