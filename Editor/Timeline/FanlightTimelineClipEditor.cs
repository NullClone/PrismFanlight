using System;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTimelineClipAsset))]
    internal sealed class FanlightTimelineClipEditor : ClipEditor
    {
        // Fields

        private const string StableClipIdPropertyName = "_stableClipId";


        // Methods

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (clonedFrom != null || clip.asset is not FanlightTimelineClipAsset asset) return;

            var serializedObject = new SerializedObject(asset);
            var stableClipId = serializedObject.FindProperty(StableClipIdPropertyName);

            if (stableClipId == null)
            {
                throw new InvalidOperationException("Timeline Clip Stable ID serialized property is missing.");
            }

            if (!string.IsNullOrWhiteSpace(stableClipId.stringValue)) return;

            stableClipId.stringValue = Guid.NewGuid().ToString("N");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
