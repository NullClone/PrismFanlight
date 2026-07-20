using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelinePlayableBehaviour : PlayableBehaviour
    {
        internal string StableClipId { get; private set; }

        internal FanlightTimelineClipValue Value { get; private set; }

        internal AnimationCurve LocalWeightCurve { get; private set; }

        internal FanlightTimelineHoldMode HoldMode { get; private set; }


        internal void Configure(
            string stableClipId,
            FanlightTimelineClipValue value,
            AnimationCurve localWeightCurve,
            FanlightTimelineHoldMode holdMode)
        {
            StableClipId = stableClipId;
            Value = value;
            LocalWeightCurve = localWeightCurve;
            HoldMode = holdMode;
        }

        internal float EvaluateLocalWeight(float normalizedTime)
        {
            if (LocalWeightCurve == null) return 1f;

            var weight = LocalWeightCurve.Evaluate(Mathf.Clamp01(normalizedTime));

            return float.IsNaN(weight) || float.IsInfinity(weight) ? 0f : Mathf.Max(0f, weight);
        }
    }
}
