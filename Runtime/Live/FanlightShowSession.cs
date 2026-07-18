using System;
using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    internal sealed class FanlightShowSession
    {
        private const int DefaultStateCapacity = 16;

        private readonly List<IFanlightContributionSource> _timelineSources = new();
        private readonly Dictionary<string, FanlightCueDefinition> _cueDefinitions;
        private readonly Dictionary<string, string> _cueSourceIds;
        private readonly Dictionary<string, CueRuntimeState> _cueStates;
        private readonly Dictionary<string, LiveRuntimeState> _liveStates;
        private readonly Dictionary<string, SafetyRuntimeState> _safetyStates;
        private readonly FanlightContributionBuffer _buffer;
        private readonly FanlightShowEventLog _eventLog;
        private string _requestedTimeProviderId = string.Empty;

        internal string ShowId { get; }
        internal string SessionId { get; }
        internal FanlightShowEventLog EventLog => _eventLog;
        internal string RequestedTimeProviderId => _requestedTimeProviderId;

        internal FanlightShowSession(
            string showId,
            string sessionId,
            IEnumerable<FanlightCueDefinition> cueDefinitions = null,
            FanlightShowEventLog eventLog = null,
            int contributionCapacity = 32)
        {
            if (string.IsNullOrWhiteSpace(showId)) throw new ArgumentException("Show ID is required.", nameof(showId));
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Session ID is required.", nameof(sessionId));
            ShowId = showId;
            SessionId = sessionId;
            _buffer = new FanlightContributionBuffer(contributionCapacity);
            _eventLog = eventLog ?? new FanlightShowEventLog();
            _cueDefinitions = new Dictionary<string, FanlightCueDefinition>(DefaultStateCapacity, StringComparer.Ordinal);
            _cueSourceIds = new Dictionary<string, string>(DefaultStateCapacity, StringComparer.Ordinal);
            _cueStates = new Dictionary<string, CueRuntimeState>(DefaultStateCapacity, StringComparer.Ordinal);
            _liveStates = new Dictionary<string, LiveRuntimeState>(DefaultStateCapacity, StringComparer.Ordinal);
            _safetyStates = new Dictionary<string, SafetyRuntimeState>(DefaultStateCapacity, StringComparer.Ordinal);
            if (cueDefinitions != null)
            {
                foreach (var definition in cueDefinitions)
                {
                    definition.Validate();
                    if (!_cueDefinitions.TryAdd(definition.CueId, definition))
                        throw new ArgumentException($"Duplicate cue ID: {definition.CueId}", nameof(cueDefinitions));
                    _cueSourceIds.Add(definition.CueId, $"cue:{definition.CueId}");
                }
            }

            for (var i = 0; i < _eventLog.Count; i++) ValidateCommand(_eventLog.GetAt(i).Command);
        }

        internal void RegisterSource(IFanlightContributionSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!_timelineSources.Contains(source)) _timelineSources.Add(source);
        }

        internal void UnregisterSource(IFanlightContributionSource source) => _timelineSources.Remove(source);

        internal FanlightShowEventLogEntry SubmitCommand(
            in FanlightShowTimeSample time,
            string stableEventId,
            FanlightShowCommand command)
        {
            if (!time.IsComplete) throw new ArgumentException("A complete time sample is required.", nameof(time));
            ValidateCommand(command);
            return _eventLog.Append(time.Seconds, stableEventId, command);
        }

        internal FanlightShowSample Evaluate(
            in FanlightShowTimeSample time,
            in FanlightShowState baseState,
            FanlightShowEvaluator evaluator,
            FanlightEvaluationOptions options)
        {
            if (!time.IsComplete) throw new ArgumentException("A complete time sample is required.", nameof(time));
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            RebuildEventState(time.Seconds);
            _buffer.Clear();
            for (var i = 0; i < _timelineSources.Count; i++) _timelineSources[i].Collect(time.Seconds, _buffer);
            AddCueContributions(time.Seconds);
            AddLiveContributions(time.Seconds);
            AddSafetyContributions();
            var request = new FanlightShowEvaluationRequest(time, baseState, _buffer.AsMemory(), options);
            return evaluator.Evaluate(request);
        }

        private void ValidateCommand(FanlightShowCommand command)
        {
            command.Validate();
            var cueId = command.Kind switch
            {
                FanlightShowCommandKind.TriggerCue => command.TriggerCueCommand.CueId,
                FanlightShowCommandKind.CancelCue => command.CancelCueCommand.CueId,
                _ => string.Empty
            };
            if (cueId.Length > 0 && !_cueDefinitions.ContainsKey(cueId))
                throw new InvalidOperationException($"Unknown cue ID: {cueId}");
        }

        private void RebuildEventState(double showSeconds)
        {
            _cueStates.Clear();
            _liveStates.Clear();
            _safetyStates.Clear();
            _requestedTimeProviderId = string.Empty;
            for (var i = 0; i < _eventLog.Count; i++)
            {
                var entry = _eventLog.GetAt(i);
                if (entry.ShowSeconds > showSeconds) break;
                Apply(entry);
            }
        }

        private void Apply(FanlightShowEventLogEntry entry)
        {
            var command = entry.Command;
            switch (command.Kind)
            {
                case FanlightShowCommandKind.SetLivePatch:
                    ApplySetLive(entry.ShowSeconds, command.SetLivePatchCommand);
                    break;
                case FanlightShowCommandKind.ClearLivePatch:
                    ApplyClearLive(entry.ShowSeconds, command.ClearLivePatchCommand);
                    break;
                case FanlightShowCommandKind.TriggerCue:
                    ApplyTriggerCue(entry.ShowSeconds, command.TriggerCueCommand.CueId);
                    break;
                case FanlightShowCommandKind.CancelCue:
                    ApplyCancelCue(entry.ShowSeconds, command.CancelCueCommand.CueId);
                    break;
                case FanlightShowCommandKind.SetSafetyPatch:
                    var setSafety = command.SetSafetyPatchCommand;
                    _safetyStates[setSafety.SourceId] = new SafetyRuntimeState(entry.ShowSeconds, setSafety.Patch);
                    break;
                case FanlightShowCommandKind.ClearSafetyPatch:
                    _safetyStates.Remove(command.ClearSafetyPatchCommand.SourceId);
                    break;
                case FanlightShowCommandKind.SelectTimeProvider:
                    _requestedTimeProviderId = command.SelectTimeProviderCommand.ProviderId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind));
            }
        }

        private void ApplySetLive(double showSeconds, FanlightSetLivePatchCommand command)
        {
            _liveStates[command.SourceId] = new LiveRuntimeState(
                showSeconds,
                command.TransitionSeconds,
                command.Priority,
                command.Patch,
                false,
                0d,
                0d,
                0f);
        }

        private void ApplyClearLive(double showSeconds, FanlightClearLivePatchCommand command)
        {
            if (!_liveStates.TryGetValue(command.SourceId, out var state)) return;
            var clearStartWeight = EvaluateLiveWeight(state, showSeconds);
            _liveStates[command.SourceId] = new LiveRuntimeState(
                state.SetSeconds,
                state.SetTransitionSeconds,
                state.Priority,
                state.Patch,
                true,
                showSeconds,
                command.TransitionSeconds,
                clearStartWeight);
        }

        private void ApplyTriggerCue(double showSeconds, string cueId)
        {
            var definition = _cueDefinitions[cueId];
            if (_cueStates.TryGetValue(cueId, out var state)
                && definition.RetriggerMode == FanlightCueRetriggerMode.IgnoreWhileActive
                && IsCueActive(definition, state, showSeconds))
                return;
            _cueStates[cueId] = new CueRuntimeState(showSeconds, false, 0d, 0f);
        }

        private void ApplyCancelCue(double showSeconds, string cueId)
        {
            if (!_cueStates.TryGetValue(cueId, out var state)) return;
            var definition = _cueDefinitions[cueId];
            if (!IsCueActive(definition, state, showSeconds)) return;
            var cancelStartWeight = EvaluateCueWeight(definition, state, showSeconds);
            _cueStates[cueId] = new CueRuntimeState(state.TriggerSeconds, true, showSeconds, cancelStartWeight);
        }

        private void AddCueContributions(double showSeconds)
        {
            foreach (var pair in _cueStates)
            {
                var definition = _cueDefinitions[pair.Key];
                var state = pair.Value;
                var endSeconds = ResolveCueEnd(definition, state);
                if (endSeconds <= state.TriggerSeconds || showSeconds < state.TriggerSeconds || showSeconds >= endSeconds)
                    continue;
                _buffer.Add(new FanlightShowContribution(
                    _cueSourceIds[pair.Key],
                    FanlightContributionLayer.Cue,
                    definition.Priority,
                    state.TriggerSeconds,
                    endSeconds,
                    EvaluateCueWeight(definition, state, showSeconds),
                    definition.Patch));
            }
        }

        private void AddLiveContributions(double showSeconds)
        {
            foreach (var pair in _liveStates)
            {
                var state = pair.Value;
                var endSeconds = state.IsClearing
                    ? AddSeconds(state.ClearSeconds, state.ClearTransitionSeconds)
                    : double.PositiveInfinity;
                if (endSeconds <= state.SetSeconds || showSeconds < state.SetSeconds || showSeconds >= endSeconds)
                    continue;
                _buffer.Add(new FanlightShowContribution(
                    pair.Key,
                    FanlightContributionLayer.Live,
                    state.Priority,
                    state.SetSeconds,
                    endSeconds,
                    EvaluateLiveWeight(state, showSeconds),
                    state.Patch));
            }
        }

        private void AddSafetyContributions()
        {
            foreach (var pair in _safetyStates)
            {
                var state = pair.Value;
                _buffer.Add(new FanlightShowContribution(
                    pair.Key,
                    FanlightContributionLayer.Safety,
                    0,
                    state.SetSeconds,
                    double.PositiveInfinity,
                    1f,
                    state.Patch));
            }
        }

        private static bool IsCueActive(FanlightCueDefinition definition, CueRuntimeState state, double showSeconds)
        {
            var endSeconds = ResolveCueEnd(definition, state);
            return showSeconds >= state.TriggerSeconds && showSeconds < endSeconds;
        }

        private static double ResolveCueReleaseStart(FanlightCueDefinition definition, CueRuntimeState state)
        {
            var automaticRelease = double.IsPositiveInfinity(definition.HoldSeconds)
                ? double.PositiveInfinity
                : AddSeconds(AddSeconds(state.TriggerSeconds, definition.AttackSeconds), definition.HoldSeconds);
            return state.IsCancelled ? Math.Min(automaticRelease, state.CancelSeconds) : automaticRelease;
        }

        private static double ResolveCueEnd(FanlightCueDefinition definition, CueRuntimeState state)
        {
            var releaseStart = ResolveCueReleaseStart(definition, state);
            return double.IsPositiveInfinity(releaseStart)
                ? double.PositiveInfinity
                : AddSeconds(releaseStart, definition.ReleaseSeconds);
        }

        private static float EvaluateCueWeight(FanlightCueDefinition definition, CueRuntimeState state, double showSeconds)
        {
            var attackWeight = definition.AttackSeconds <= 0d
                ? 1f
                : Clamp01((showSeconds - state.TriggerSeconds) / definition.AttackSeconds);
            var releaseStart = ResolveCueReleaseStart(definition, state);
            if (double.IsPositiveInfinity(releaseStart) || showSeconds < releaseStart) return attackWeight;
            if (definition.ReleaseSeconds <= 0d) return 0f;
            var releaseWeight = Clamp01((ResolveCueEnd(definition, state) - showSeconds) / definition.ReleaseSeconds);
            return IsCancellationRelease(definition, state)
                ? state.CancelStartWeight * releaseWeight
                : releaseWeight;
        }

        private static bool IsCancellationRelease(FanlightCueDefinition definition, CueRuntimeState state)
        {
            if (!state.IsCancelled) return false;
            if (double.IsPositiveInfinity(definition.HoldSeconds)) return true;
            var automaticRelease = AddSeconds(
                AddSeconds(state.TriggerSeconds, definition.AttackSeconds),
                definition.HoldSeconds);
            return state.CancelSeconds < automaticRelease;
        }

        private static float EvaluateLiveWeight(LiveRuntimeState state, double showSeconds)
        {
            var attackWeight = state.SetTransitionSeconds <= 0d
                ? 1f
                : Clamp01((showSeconds - state.SetSeconds) / state.SetTransitionSeconds);
            if (!state.IsClearing) return attackWeight;
            if (state.ClearTransitionSeconds <= 0d) return 0f;
            var releaseWeight = 1f - Clamp01((showSeconds - state.ClearSeconds) / state.ClearTransitionSeconds);
            return state.ClearStartWeight * releaseWeight;
        }

        private static double AddSeconds(double start, double duration)
        {
            if (double.IsPositiveInfinity(start) || double.IsPositiveInfinity(duration)) return double.PositiveInfinity;
            var value = start + duration;
            return double.IsPositiveInfinity(value) ? double.PositiveInfinity : value;
        }

        private static float Clamp01(double value) => value <= 0d ? 0f : value >= 1d ? 1f : (float)value;

        private readonly struct CueRuntimeState
        {
            internal double TriggerSeconds { get; }
            internal bool IsCancelled { get; }
            internal double CancelSeconds { get; }
            internal float CancelStartWeight { get; }

            internal CueRuntimeState(
                double triggerSeconds,
                bool isCancelled,
                double cancelSeconds,
                float cancelStartWeight)
            {
                TriggerSeconds = triggerSeconds;
                IsCancelled = isCancelled;
                CancelSeconds = cancelSeconds;
                CancelStartWeight = cancelStartWeight;
            }
        }

        private readonly struct LiveRuntimeState
        {
            internal double SetSeconds { get; }
            internal double SetTransitionSeconds { get; }
            internal int Priority { get; }
            internal FanlightShowPatch Patch { get; }
            internal bool IsClearing { get; }
            internal double ClearSeconds { get; }
            internal double ClearTransitionSeconds { get; }
            internal float ClearStartWeight { get; }

            internal LiveRuntimeState(
                double setSeconds,
                double setTransitionSeconds,
                int priority,
                FanlightShowPatch patch,
                bool isClearing,
                double clearSeconds,
                double clearTransitionSeconds,
                float clearStartWeight)
            {
                SetSeconds = setSeconds;
                SetTransitionSeconds = setTransitionSeconds;
                Priority = priority;
                Patch = patch;
                IsClearing = isClearing;
                ClearSeconds = clearSeconds;
                ClearTransitionSeconds = clearTransitionSeconds;
                ClearStartWeight = clearStartWeight;
            }
        }

        private readonly struct SafetyRuntimeState
        {
            internal double SetSeconds { get; }
            internal FanlightShowPatch Patch { get; }

            internal SafetyRuntimeState(double setSeconds, FanlightShowPatch patch)
            {
                SetSeconds = setSeconds;
                Patch = patch;
            }
        }
    }
}
