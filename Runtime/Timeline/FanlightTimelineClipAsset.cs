using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineClipAsset : PlayableAsset, ITimelineClipAsset
    {
        // Properties

        public ClipCaps clipCaps =>
            ClipCaps.Blending |
            ClipCaps.ClipIn |
            ClipCaps.SpeedMultiplier |
            ClipCaps.Extrapolation;

        internal abstract FanlightTimelineClipValue Value { get; }


        // Methods

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            playable.GetBehaviour().Configure(Value);
            return playable;
        }
    }
}
