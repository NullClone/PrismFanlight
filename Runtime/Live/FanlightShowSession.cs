using System;
using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    public sealed class FanlightLiveOverrideSource : IFanlightContributionSource
    {
        private readonly FanlightMutableEventLog _eventLog;
        private readonly Dictionary<string, FanlightLiveEvent> _active = new(StringComparer.Ordinal);
        private long _eventSequence;

        public FanlightLiveOverrideSource(FanlightMutableEventLog eventLog, string sourceId = "live.override", int priority = 0)
        {
            _eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            SourceId = sourceId ?? "live.override";
            Priority = priority;
        }

        public string SourceId { get; }
        public FanlightContributionLayer Layer => FanlightContributionLayer.Live;
        public int Priority { get; }

        public void CreateOrUpdate(string overrideId, double showSeconds, FanlightIntentPatch patch, bool update = false)
        {
            if (string.IsNullOrEmpty(overrideId)) throw new ArgumentException("Override ID is required.", nameof(overrideId));
            var sequence = _eventSequence + 1;
            _eventLog.Append(new FanlightLiveEvent(
                $"{SourceId}:{overrideId}:{sequence}",
                SourceId,
                update ? FanlightLiveEventType.LiveOverrideUpdated : FanlightLiveEventType.LiveOverrideCreated,
                showSeconds,
                sequence,
                overrideId,
                string.Empty,
                0,
                patch,
                true,
                string.Empty,
                1));
            _eventSequence = sequence;
        }

        public void Release(string overrideId, double showSeconds)
        {
            var sequence = _eventSequence + 1;
            _eventLog.Append(new FanlightLiveEvent(
                $"{SourceId}:{overrideId}:{sequence}",
                SourceId,
                FanlightLiveEventType.LiveOverrideReleased,
                showSeconds,
                sequence,
                overrideId,
                string.Empty,
                0,
                default,
                false,
                string.Empty,
                1));
            _eventSequence = sequence;
        }

        public void Collect(double seconds, FanlightContributionBuffer destination)
        {
            _active.Clear();
            for (var i = 0; i < _eventLog.Count; i++)
            {
                var value = _eventLog.GetAt(i);
                if (value.ShowSeconds > seconds) break;
                if (value.SourceId != SourceId) continue;
                if (value.EventType == FanlightLiveEventType.LiveOverrideReleased) _active.Remove(value.TargetId);
                else if (value.EventType is FanlightLiveEventType.LiveOverrideCreated or FanlightLiveEventType.LiveOverrideUpdated)
                    _active[value.TargetId] = value;
            }

            foreach (var pair in _active)
            {
                var value = pair.Value;
                destination.Add(new FanlightContribution(
                    $"override:{pair.Key}",
                    SourceId,
                    Layer,
                    Priority,
                    value.ShowSeconds,
                    double.PositiveInfinity,
                    0d,
                    0d,
                    1f,
                    FanlightBlendProfile.Linear,
                    FanlightReleasePolicy.RestoreUnderlying,
                    value.Patch));
            }
        }
    }

    public sealed class FanlightShowSession
    {
        private readonly List<IFanlightContributionSource> _sources = new();
        private readonly List<IFanlightCueSession> _cueSessions = new();
        private readonly FanlightContributionBuffer _buffer;
        private readonly FanlightMutableEventLog _eventLog;
        private readonly FanlightTimeEventRecorder _timeRecorder;

        public FanlightShowSession(string showId, string sessionId, int contributionCapacity = 32)
        {
            ShowId = showId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            _buffer = new FanlightContributionBuffer(contributionCapacity);
            _eventLog = new FanlightMutableEventLog(ShowId, SessionId);
            _timeRecorder = new FanlightTimeEventRecorder(_eventLog);
            LiveOverrides = new FanlightLiveOverrideSource(_eventLog);
            RegisterSource(LiveOverrides);
        }

        public string ShowId { get; }
        public string SessionId { get; }
        public FanlightLiveOverrideSource LiveOverrides { get; }
        public FanlightContributionBuffer Contributions => _buffer;
        public FanlightLiveEventLog EventLog => _eventLog;

        public void RegisterSource(IFanlightContributionSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (_sources.Contains(source)) return;
            _sources.Add(source);
        }

        public void UnregisterSource(IFanlightContributionSource source) => _sources.Remove(source);

        public void RegisterCueSession(IFanlightCueSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (!_cueSessions.Contains(session)) _cueSessions.Add(session);
        }

        public void UnregisterCueSession(IFanlightCueSession session) => _cueSessions.Remove(session);

        public FanlightShowSample Evaluate(
            in FanlightShowTimeSample time,
            in FanlightShowSnapshot snapshot,
            IFanlightShowEvaluator evaluator,
            FanlightEvaluationOptions options)
        {
            if (!time.IsComplete) throw new ArgumentException("A complete time sample is required.", nameof(time));
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            _timeRecorder.Observe(time);
            _buffer.Clear();
            for (var i = 0; i < _sources.Count; i++)
            {
                var start = _buffer.Count;
                _sources[i].Collect(time.Seconds, _buffer);
                for (var index = start; index < _buffer.Count; index++)
                {
                    var contribution = _buffer.GetAt(index);
                    if (!string.Equals(contribution.SourceId, _sources[i].SourceId, StringComparison.Ordinal)
                        || contribution.Layer != _sources[i].Layer
                        || contribution.Priority != _sources[i].Priority)
                        throw new InvalidOperationException($"Contribution source contract mismatch: {_sources[i].SourceId}");
                }
            }

            for (var i = 0; i < _cueSessions.Count; i++)
            {
                _cueSessions[i].CollectContributions(time.Seconds, _buffer);
            }

            var request = new FanlightShowEvaluationRequest(snapshot, time, _buffer, _eventLog, evaluator.SchemaVersion, options);
            return evaluator.Evaluate(request);
        }
    }
}
