using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTempoClip))]
    internal sealed class FanlightTempoClipEditor : ClipEditor
    {
        // Methods

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);

            if (clip.asset is not FanlightTempoClip tempoClip) return options;

            options.highlightColor = new Color(0.18f, 0.55f, 0.85f, 1f);
            options.tooltip = $"{tempoClip.Bpm:0.###} BPM";

            if (!tempoClip.TryValidate(out var error))
            {
                options.errorText = error;
            }

            return options;
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            UpdateDisplayName(clip);
        }

        public override void OnClipChanged(TimelineClip clip)
        {
            UpdateDisplayName(clip);
        }

        private static void UpdateDisplayName(TimelineClip clip)
        {
            if (clip.asset is FanlightTempoClip tempoClip)
            {
                clip.displayName = $"{tempoClip.Bpm:0.###} BPM";
            }
        }
    }
}
