using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public abstract class FanlightTimelineClipAsset : PlayableAsset, ITimelineClipAsset
    {
        // Properties

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn;

        internal abstract FanlightTimelineClipValue Value { get; }


        // Methods

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FanlightTimelinePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            try
            {
                behaviour.Configure(Value);
            }
            catch (ArgumentException exception)
            {
                behaviour.ConfigureFault(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                behaviour.ConfigureFault(exception.Message);
            }

            return playable;
        }
    }
}
