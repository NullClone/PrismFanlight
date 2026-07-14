using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightPaletteGradientPlayableAsset))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(1.0f, 0.35f, 0.65f)]
    public sealed class FanlightPaletteGradientTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<FanlightPaletteGradientMixerBehaviour>.Create(graph, inputCount);
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
