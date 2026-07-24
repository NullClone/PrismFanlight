using System;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightColorClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(1.00f, 0.35f, 0.65f)]
    public sealed class FanlightColorTrack : FanlightTimelineTrackAsset
    {
        // Fields

        [SerializeField]
        private FanlightColorFields _fields = FanlightColorFields.All;


        // Properties

        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Color;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);


        // Methods

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            if (timelineAsset == null)
            {
                throw new InvalidOperationException("Color tracks must belong to a Timeline Asset.");
            }

            var trackCount = 0;
            foreach (var outputTrack in timelineAsset.GetOutputTracks())
            {
                if (outputTrack is FanlightColorTrack) trackCount++;
            }

            if (trackCount > 1)
            {
                throw new InvalidOperationException("A Timeline Asset can contain only one Prism Fanlight Color Track.");
            }

            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }
}
