using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        internal FanlightTimelineClipValue Value { get; private set; }

        internal string Fault { get; private set; }

        internal void Configure(FanlightTimelineClipValue value)
        {
            Value = value;
            Fault = string.Empty;
        }

        internal void ConfigureFault(string fault)
        {
            Value = default;
            Fault = string.IsNullOrEmpty(fault) ? "Timeline Clip contains an invalid value." : fault;
        }
    }
}
