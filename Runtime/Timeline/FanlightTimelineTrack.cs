using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightTimelinePlayableAsset))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.25f, 0.6f, 1.0f)]
    public sealed class FanlightTimelineTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<FanlightTimelineMixerBehaviour>.Create(graph, inputCount);
            mixer.GetBehaviour().Configure(GetTrackSortOrder());
            return mixer;
        }

        private int GetTrackSortOrder()
        {
            if (timelineAsset == null) return 0;

            var order = 0;
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track == this) return order;
                order++;
            }

            return order;
        }
    }
}
