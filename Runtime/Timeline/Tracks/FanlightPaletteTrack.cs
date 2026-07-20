using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightPaletteClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(1.00f, 0.35f, 0.65f)]
    public sealed class FanlightPaletteTrack : FanlightTimelineTrackAsset
    {
        [SerializeField]
        private FanlightPaletteFields _fields = FanlightPaletteFields.All;


        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Palette;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);
    }
}
