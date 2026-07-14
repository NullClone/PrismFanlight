using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor.Timeline
{
    [CustomTimelineEditor(typeof(FanlightTimelinePlayableAsset))]
    public sealed class FanlightTimelineClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.highlightColor = Color.clear;
            return options;
        }
    }
}
