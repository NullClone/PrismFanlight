using System;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightMotionClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.90f, 0.65f, 0.20f)]
    public sealed class FanlightMotionTrack : FanlightTimelineTrackAsset
    {
        // Fields

        [SerializeField]
        private FanlightMotionFields _fields = FanlightMotionFields.All;


        // Properties

        internal override FanlightTimelinePatchKind PatchKind => FanlightTimelinePatchKind.Motion;

        internal override FanlightTimelineFieldMask FieldMask => FanlightTimelineFieldMask.From(_fields);


        // Methods

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            if (timelineAsset == null)
            {
                throw new InvalidOperationException("Motion tracks must belong to a Timeline Asset.");
            }

            var motionTrackCount = 0;

            foreach (var outputTrack in timelineAsset.GetOutputTracks())
            {
                if (outputTrack is FanlightMotionTrack) motionTrackCount++;
            }

            if (motionTrackCount > 1)
            {
                throw new InvalidOperationException("A Timeline Asset can contain only one Prism Fanlight Motion Track.");
            }

            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }
}
