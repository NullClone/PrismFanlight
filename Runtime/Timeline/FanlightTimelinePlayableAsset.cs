using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight
{
    public sealed class FanlightTimelinePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        // Fields

        [ColorUsage(false, true)]
        public Color color = Color.white;

        [Min(0.0f)]
        public float intensity = 20.0f;


        // Methods

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);

            var behaviour = playable.GetBehaviour();
            behaviour.Color = color;
            behaviour.Intensity = Mathf.Max(0.0f, intensity);

            return playable;
        }
    }
}
