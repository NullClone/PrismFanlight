using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private const float WeightEpsilon = 0.0001f;

        private PrismFanlight _lastTarget;
        private bool _hasActiveCue;
        private string _sourceId = "timeline.unconfigured";
        private int _priority;

        private readonly FanlightTimelineTrackContribution _contribution = new();
        private readonly HashSet<string> _reportedUnsupportedPaths = new(StringComparer.Ordinal);


        // Methods

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fanlight = playerData as PrismFanlight;

            if (_lastTarget != fanlight)
            {
                if (_lastTarget != null)
                {
                    _lastTarget.ClearScheduledContribution(this);
                }

                _hasActiveCue = false;
            }

            _lastTarget = fanlight;

            if (fanlight == null) return;

            var time = (float)playable.GetTime();
            var isTimeJump = IsTimeJump(playable, info);

            _contribution.Begin(time, !_hasActiveCue || isTimeJump, _priority);

            for (var i = 0; i < playable.GetInputCount(); i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= WeightEpsilon) continue;

                var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)playable.GetInput(i);
                _contribution.Add(input.GetBehaviour(), weight);
            }

            if (!_contribution.HasOverrides)
            {
                fanlight.ClearScheduledContribution(this);
                _hasActiveCue = false;
                return;
            }

            var patch = _contribution.BuildPatch(fanlight.BaseIntent);
            ReportUnsupportedPaths();
            if (!_contribution.HasMappedOverrides)
            {
                fanlight.ClearScheduledContribution(this);
                _hasActiveCue = false;
                return;
            }
            var contribution = new FanlightContribution(
                $"{_sourceId}:active",
                _sourceId,
                FanlightContributionLayer.Scheduled,
                _priority,
                double.MinValue,
                double.MaxValue,
                0d,
                0d,
                1f,
                FanlightBlendProfile.Linear,
                FanlightReleasePolicy.RestoreUnderlying,
                patch);
            fanlight.SetScheduledContribution(this, contribution);
            _hasActiveCue = true;
        }

        public void Configure(string sourceId, int priority)
        {
            _sourceId = sourceId;
            _priority = priority;
        }

        private void ReportUnsupportedPaths()
        {
            for (var i = 0; i < _contribution.UnsupportedPaths.Count; i++)
            {
                var path = _contribution.UnsupportedPaths[i];
                if (!_reportedUnsupportedPaths.Add(path)) continue;
                UnityEngine.Debug.LogWarning(
                    $"Prism Fanlight legacy Timeline override '{path}' has no Stage 1 intent mapping. " +
                    "Tempo overrides must be converted to a FanlightTempoMap; other paths require an approved Expert parameter ID.");
            }
        }

        private static bool IsTimeJump(Playable playable, FrameData info)
        {
            if (info.seekOccurred || info.timeLooped || info.evaluationType == FrameData.EvaluationType.Evaluate) return true;

            var actualDelta = playable.GetTime() - playable.GetPreviousTime();
            var expectedDelta = info.deltaTime * info.effectiveSpeed;

            return Math.Abs(actualDelta - expectedDelta) > 0.000001;
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledContribution(this);
            }

            _lastTarget = null;
            _hasActiveCue = false;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledContribution(this);
            }

            _lastTarget = null;
            _hasActiveCue = false;
        }
    }
}
