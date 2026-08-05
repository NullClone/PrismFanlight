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
            var error = FanlightTimelineTrackEditor.GetClipError(clip);
            options.errorText = FanlightTimelineTrackEditor.AppendError(options.errorText, error);
            return options;
        }
    }
}
