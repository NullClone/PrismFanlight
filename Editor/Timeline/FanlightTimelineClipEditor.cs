using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTimelineClipAsset))]
    internal sealed class FanlightTimelineClipEditor : ClipEditor
    {
        // Methods

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.errorText = FanlightTimelineTrackEditor.AppendError(
                options.errorText,
                FanlightTimelineTrackEditor.GetClipError(clip));
            return options;
        }
    }
}
