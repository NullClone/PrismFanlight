using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        internal FanlightTimelineClipValue Value { get; private set; }

        internal void Configure(FanlightTimelineClipValue value)
        {
            Value = value;
        }
    }
}
