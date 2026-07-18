using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        private const float WeightEpsilon = 0.0001f;

        private PrismFanlight _lastTarget;
        private string _sourceId = "timeline.unconfigured";
        private FanlightTimelinePatchKind _patchKind;
        private FanlightTimelineClipRange[] _ranges = Array.Empty<FanlightTimelineClipRange>();
        private FanlightTimelineClipSample[] _samples = Array.Empty<FanlightTimelineClipSample>();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as PrismFanlight;
            if (_lastTarget != target)
            {
                if (_lastTarget != null) _lastTarget.ClearScheduledContribution(this);
                _lastTarget = target;
            }

            if (target == null) return;
            var inputCount = playable.GetInputCount();
            EnsureSampleCapacity(inputCount);
            var sampleCount = 0;
            var priority = int.MinValue;
            var timelineSeconds = playable.GetTime();

            for (var i = 0; i < inputCount; i++)
            {
                var inputPlayable = playable.GetInput(i);
                var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)inputPlayable;
                var behaviour = input.GetBehaviour();
                var timelineWeight = playable.GetInputWeight(i);
                var held = false;
                if (timelineWeight <= WeightEpsilon
                    && behaviour.HoldMode == FanlightTimelineHoldMode.HoldLast
                    && i < _ranges.Length
                    && timelineSeconds >= _ranges[i].EndSeconds
                    && timelineSeconds < _ranges[i].HoldEndSeconds)
                {
                    timelineWeight = 1f;
                    held = true;
                }

                if (float.IsNaN(timelineWeight) || float.IsInfinity(timelineWeight) || timelineWeight <= WeightEpsilon)
                    continue;
                var duration = inputPlayable.GetDuration();
                var normalizedTime = held || duration <= 0d || double.IsInfinity(duration)
                    ? held ? 1f : 0f
                    : (float)(inputPlayable.GetTime() / duration);
                var weight = timelineWeight * behaviour.EvaluateLocalWeight(normalizedTime);
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= WeightEpsilon) continue;
                if (!FanlightTimelinePatchMixer.HasFields(_patchKind, behaviour.Patch)) continue;
                var stableClipId = i < _ranges.Length && !string.IsNullOrWhiteSpace(_ranges[i].StableClipId)
                    ? _ranges[i].StableClipId
                    : behaviour.StableClipId;
                _samples[sampleCount++] = new FanlightTimelineClipSample(
                    stableClipId,
                    behaviour.Patch,
                    weight,
                    behaviour.Priority);
                priority = Math.Max(priority, behaviour.Priority);
            }

            if (sampleCount == 0)
            {
                target.ClearScheduledContribution(this);
                return;
            }

            Array.Sort(_samples, 0, sampleCount, SampleComparer.Instance);
            try
            {
                if (!FanlightTimelinePatchMixer.TryBlend(_patchKind, _samples.AsSpan(0, sampleCount), out var patch))
                {
                    target.ClearScheduledContribution(this);
                    return;
                }

                target.SetScheduledContribution(
                    this,
                    new FanlightShowContribution(
                        _sourceId,
                        FanlightContributionLayer.Timeline,
                        priority,
                        double.MinValue,
                        double.MaxValue,
                        1f,
                        patch));
            }
            finally
            {
                ClearSamples(sampleCount);
            }
        }

        public override void OnGraphStop(Playable playable) => ClearContribution();

        public override void OnPlayableDestroy(Playable playable) => ClearContribution();

        internal void Configure(
            string sourceId,
            FanlightTimelinePatchKind patchKind,
            FanlightTimelineClipRange[] ranges)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", nameof(sourceId));
            _sourceId = sourceId;
            _patchKind = patchKind;
            _ranges = ranges ?? Array.Empty<FanlightTimelineClipRange>();
        }

        private void EnsureSampleCapacity(int capacity)
        {
            if (_samples.Length >= capacity) return;
            Array.Resize(ref _samples, Math.Max(4, capacity));
        }

        private void ClearSamples(int count)
        {
            if (count > 0) Array.Clear(_samples, 0, count);
        }

        private void ClearContribution()
        {
            if (_lastTarget != null) _lastTarget.ClearScheduledContribution(this);
            _lastTarget = null;
        }

        private sealed class SampleComparer : IComparer<FanlightTimelineClipSample>
        {
            internal static readonly SampleComparer Instance = new();

            public int Compare(FanlightTimelineClipSample left, FanlightTimelineClipSample right) =>
                string.Compare(left.StableClipId, right.StableClipId, StringComparison.Ordinal);
        }
    }
}
