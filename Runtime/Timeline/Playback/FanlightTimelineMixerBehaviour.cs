using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private string _sourceId = "timeline.unconfigured";

        private PrismFanlight _lastTarget;

        private FanlightTimelinePatchKind _patchKind;
        private FanlightTimelineFieldMask _fieldMask;
        private int _priority;

        private FanlightTimelineClipRange[] _ranges = Array.Empty<FanlightTimelineClipRange>();
        private FanlightTimelineClipSample[] _samples = Array.Empty<FanlightTimelineClipSample>();


        // Methods

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as PrismFanlight;

            if (_lastTarget != target)
            {
                if (_lastTarget != null)
                {
                    _lastTarget.ClearScheduledContribution(this);
                }

                _lastTarget = target;
            }

            if (target == null) return;

            var inputCount = playable.GetInputCount();

            EnsureSampleCapacity(inputCount);

            var sampleCount = 0;
            var totalWeight = 0f;
            var timelineSeconds = playable.GetTime();

            for (var i = 0; i < inputCount; i++)
            {
                var inputPlayable = playable.GetInput(i);
                var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)inputPlayable;
                var behaviour = input.GetBehaviour();
                var timelineWeight = playable.GetInputWeight(i);
                var held = false;

                if (timelineWeight <= 0f
                    && behaviour.HoldMode == FanlightTimelineHoldMode.HoldLast
                    && i < _ranges.Length
                    && timelineSeconds >= _ranges[i].EndSeconds
                    && timelineSeconds < _ranges[i].HoldEndSeconds)
                {
                    timelineWeight = 1f;
                    held = true;
                }

                if (float.IsNaN(timelineWeight) || float.IsInfinity(timelineWeight) || timelineWeight <= 0f) continue;

                var duration = inputPlayable.GetDuration();
                var normalizedTime = held || duration <= 0d || double.IsInfinity(duration)
                    ? held ? 1f : 0f
                    : (float)(inputPlayable.GetTime() / duration);
                var weight = timelineWeight * behaviour.EvaluateLocalWeight(normalizedTime);

                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f) continue;
                if (!FanlightTimelinePatchMixer.HasFields(_patchKind, _fieldMask)) continue;

                var stableClipId = i < _ranges.Length && !string.IsNullOrWhiteSpace(_ranges[i].StableClipId)
                    ? _ranges[i].StableClipId
                    : behaviour.StableClipId;

                _samples[sampleCount++] = new FanlightTimelineClipSample(
                    stableClipId,
                    behaviour.Value,
                    weight);

                totalWeight += weight;
            }

            if (sampleCount == 0)
            {
                target.ClearScheduledContribution(this);
                return;
            }

            Array.Sort(_samples, 0, sampleCount, SampleComparer.Instance);

            try
            {
                if (!FanlightTimelinePatchMixer.TryBlend(
                        _patchKind,
                        _fieldMask,
                        _samples.AsSpan(0, sampleCount),
                        out var patch))
                {
                    target.ClearScheduledContribution(this);
                    return;
                }

                target.SetScheduledContribution(
                    this,
                    new FanlightShowContribution(
                        _sourceId,
                        _priority,
                        double.MinValue,
                        double.MaxValue,
                        Mathf.Clamp01(totalWeight),
                        patch));
            }
            finally
            {
                ClearSamples(sampleCount);
            }
        }

        public override void OnPlayableDestroy(Playable playable) => ClearContribution();

        internal void Configure(
            string sourceId,
            FanlightTimelinePatchKind patchKind,
            FanlightTimelineFieldMask fieldMask,
            int priority,
            FanlightTimelineClipRange[] ranges)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source ID is required.", nameof(sourceId));
            }

            _sourceId = sourceId;
            _patchKind = patchKind;
            _fieldMask = fieldMask;
            _priority = priority;
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
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledContribution(this);
            }

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
