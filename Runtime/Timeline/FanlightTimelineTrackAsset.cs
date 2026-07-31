using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineTrackAsset : TrackAsset
    {
        // Fields

        [SerializeField]
        private int _trackPriority;


        // Properties

        internal abstract FanlightTimelinePatchKind PatchKind { get; }

        internal abstract FanlightTimelineFieldMask FieldMask { get; }


        // Methods

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var clipStartSeconds = new double[inputCount];
            var index = 0;

            foreach (var clip in GetClips())
            {
                if (index >= inputCount)
                {
                    throw new InvalidOperationException("Timeline clip input count does not match the authored track.");
                }

                if (clip.asset is not FanlightTimelineClipAsset asset || !IsMatchingClip(asset))
                {
                    throw new InvalidOperationException("Typed Timeline tracks require matching typed clips.");
                }

                clipStartSeconds[index++] = clip.start;
            }

            if (index != inputCount)
            {
                throw new InvalidOperationException("Timeline clip input count does not match the authored track.");
            }

            var mixer = ScriptPlayable<FanlightTimelineMixerBehaviour>.Create(graph, inputCount);
            var director = graph.GetResolver() as PlayableDirector;

            if (director == null)
            {
                throw new InvalidOperationException("Timeline Track requires a PlayableDirector graph resolver.");
            }

            mixer.GetBehaviour().Configure(
                PatchKind,
                FieldMask,
                _trackPriority,
                GetTrackOrder(),
                clipStartSeconds,
                director);

            return mixer;
        }

        private int GetTrackOrder()
        {
            if (timelineAsset == null)
            {
                throw new InvalidOperationException("Timeline tracks must belong to a Timeline Asset.");
            }

            var trackOrder = 0;

            foreach (var outputTrack in timelineAsset.GetOutputTracks())
            {
                if (outputTrack == this) return trackOrder;
                trackOrder++;
            }

            throw new InvalidOperationException("Timeline Track Order could not be resolved from the Timeline Asset output tracks.");
        }

        private bool IsMatchingClip(FanlightTimelineClipAsset asset)
        {
            var attributes = GetType().GetCustomAttributes(typeof(TrackClipTypeAttribute), true);

            for (var i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] is TrackClipTypeAttribute attribute && attribute.inspectedType.IsInstanceOfType(asset))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
