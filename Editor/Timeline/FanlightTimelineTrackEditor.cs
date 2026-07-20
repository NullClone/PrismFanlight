using System;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTimelineTrackAsset))]
    internal sealed class FanlightTimelineTrackEditor : TrackEditor
    {
        // Fields

        private const string StableTrackIdPropertyName = "_stableTrackId";


        // Methods

        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            if (copiedFrom != null || track is not FanlightTimelineTrackAsset) return;

            var serializedObject = new SerializedObject(track);
            var stableTrackId = serializedObject.FindProperty(StableTrackIdPropertyName);

            if (stableTrackId == null)
            {
                throw new InvalidOperationException("Timeline Track Stable ID serialized property is missing.");
            }

            if (!string.IsNullOrWhiteSpace(stableTrackId.stringValue)) return;

            stableTrackId.stringValue = Guid.NewGuid().ToString("N");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
