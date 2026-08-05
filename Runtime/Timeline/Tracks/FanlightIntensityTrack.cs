using System;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightIntensityClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(1.00f, 0.75f, 0.20f)]
    public sealed class FanlightIntensityTrack : FanlightTimelineTrackAsset
    {
        // Fields

        [SerializeField]
        private FanlightIntensityFields _fields = FanlightIntensityFields.All;


        // Properties

        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Intensity;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);


        // Methods

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            if (timelineAsset == null)
            {
                throw new InvalidOperationException("Intensity tracks must belong to a Timeline Asset.");
            }

            var trackCount = 0;
            foreach (var outputTrack in timelineAsset.GetOutputTracks())
            {
                if (outputTrack is FanlightIntensityTrack) trackCount++;
            }

            if (trackCount > 1)
            {
                throw new InvalidOperationException("A Timeline Asset can contain only one Prism Fanlight Intensity Track.");
            }

            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }
}
