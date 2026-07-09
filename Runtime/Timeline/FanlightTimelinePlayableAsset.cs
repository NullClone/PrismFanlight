using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight
{
    public sealed class FanlightTimelinePlayableAsset : PlayableAsset
    {
        public ExposedReference<PrismFanlight> target;
        public FanlightTempoSettings tempo = FanlightTempoSettings.Default();
        public FanlightMotionPreset motionPreset;
        public FanlightColorPreset colorPreset;
        public FanlightAudienceSettings audience = FanlightAudienceSettings.Default();
        public FanlightLodSettings lod = FanlightLodSettings.Default();
        public FanlightRandomSettings random = FanlightRandomSettings.Default();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Target = target.Resolve(graph.GetResolver());
            behaviour.Tempo = tempo.Validated();
            behaviour.MotionPreset = motionPreset;
            behaviour.ColorPreset = colorPreset;
            behaviour.Audience = audience.Validated();
            behaviour.Lod = lod.Validated();
            behaviour.Random = random.Validated();
            return playable;
        }
    }
}
