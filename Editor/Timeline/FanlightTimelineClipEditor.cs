using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor.Timeline
{
    [CustomTimelineEditor(typeof(FanlightTimelinePlayableAsset))]
    public sealed class FanlightTimelineClipEditor : ClipEditor
    {
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (clonedFrom == null)
            {
                clip.displayName = "Fanlight Cue";
            }
        }
    }
}
