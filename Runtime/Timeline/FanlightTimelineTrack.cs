using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight
{
    [TrackClipType(typeof(FanlightTimelinePlayableAsset))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.25f, 0.6f, 1.0f)]
    public sealed class FanlightTimelineTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<FanlightTimelineMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
