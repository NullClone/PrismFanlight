using System;
using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    internal sealed class FanlightShowSession
    {
        private readonly List<IFanlightContributionSource> _sources = new();
        private readonly List<IFanlightCueSession> _cueSessions = new();
        private readonly FanlightContributionBuffer _buffer;
        private readonly FanlightMutableEventLog _eventLog;
        private readonly FanlightTimeEventRecorder _timeRecorder;

        internal string ShowId { get; }
        internal string SessionId { get; }
        internal FanlightLiveOverrideSource LiveOverrides { get; }
        internal FanlightContributionBuffer Contributions => _buffer;
        internal FanlightLiveEventLog EventLog => _eventLog;

        internal FanlightShowSession(string showId, string sessionId, int contributionCapacity = 32)
        {
            ShowId = showId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            _buffer = new FanlightContributionBuffer(contributionCapacity);
            _eventLog = new FanlightMutableEventLog(ShowId, SessionId);
            _timeRecorder = new FanlightTimeEventRecorder(_eventLog);
            LiveOverrides = new FanlightLiveOverrideSource(_eventLog);
            RegisterSource(LiveOverrides);
        }

        internal void RegisterSource(IFanlightContributionSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!_sources.Contains(source)) _sources.Add(source);
        }

        internal void UnregisterSource(IFanlightContributionSource source) => _sources.Remove(source);

        internal void UnregisterCueSession(IFanlightCueSession session) => _cueSessions.Remove(session);

        internal FanlightShowSample Evaluate(
            in FanlightShowTimeSample time,
            in FanlightShowState baseState,
            FanlightShowEvaluator evaluator,
            FanlightEvaluationOptions options)
        {
            if (!time.IsComplete) throw new ArgumentException("A complete time sample is required.", nameof(time));
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            _timeRecorder.Observe(time);
            _buffer.Clear();
            for (var i = 0; i < _sources.Count; i++) _sources[i].Collect(time.Seconds, _buffer);
            for (var i = 0; i < _cueSessions.Count; i++) _cueSessions[i].CollectContributions(time.Seconds, _buffer);
            var request = new FanlightShowEvaluationRequest(time, baseState, _buffer.AsMemory(), options);
            return evaluator.Evaluate(request);
        }
    }
}
