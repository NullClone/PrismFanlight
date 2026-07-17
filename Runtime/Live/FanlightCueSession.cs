using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Live
{
    public enum FanlightCueState
    {
        Idle = 0,
        Armed = 1,
        FadingIn = 2,
        Active = 3,
        FadingOut = 4,
        Completed = 5,
        Aborted = 6,
        Faulted = 7
    }

    public enum FanlightCueHoldMode
    {
        Timed = 0,
        Manual = 1,
        UntilNextCue = 2
    }

    public enum FanlightCueCommandType
    {
        Arm = 0,
        Go = 1,
        Release = 2,
        Abort = 3,
        Replace = 4,
        ClearLayer = 5,
        SafetyStop = 6,
        Resume = 7
    }

    public enum FanlightCueSafetyClassification
    {
        Normal = 0,
        Caution = 1,
        Safety = 2
    }

    public readonly struct FanlightCueDefinition
    {
        public string CueId { get; }
        public string DisplayName { get; }
        public string SourceId { get; }
        public FanlightContributionLayer Layer { get; }
        public int Priority { get; }
        public FanlightIntentPatch Patch { get; }
        public double FadeInSeconds { get; }
        public FanlightCueHoldMode HoldMode { get; }
        public double DurationSeconds { get; }
        public double FadeOutSeconds { get; }
        public FanlightReleasePolicy ReleasePolicy { get; }
        public string FollowCueId { get; }
        public string PreconditionId { get; }
        public FanlightCueSafetyClassification SafetyClassification { get; }
        public int SchemaVersion { get; }

        public FanlightCueDefinition(
            string cueId,
            string sourceId,
            int priority,
            FanlightIntentPatch patch,
            double fadeInSeconds,
            FanlightCueHoldMode holdMode,
            double durationSeconds,
            double fadeOutSeconds,
            FanlightReleasePolicy releasePolicy)
            : this(
                cueId,
                cueId,
                sourceId,
                FanlightContributionLayer.Live,
                priority,
                patch,
                fadeInSeconds,
                holdMode,
                durationSeconds,
                fadeOutSeconds,
                releasePolicy,
                string.Empty,
                string.Empty,
                FanlightCueSafetyClassification.Normal,
                1)
        {
        }

        public FanlightCueDefinition(
            string cueId,
            string displayName,
            string sourceId,
            FanlightContributionLayer layer,
            int priority,
            FanlightIntentPatch patch,
            double fadeInSeconds,
            FanlightCueHoldMode holdMode,
            double durationSeconds,
            double fadeOutSeconds,
            FanlightReleasePolicy releasePolicy,
            string followCueId,
            string preconditionId,
            FanlightCueSafetyClassification safetyClassification,
            int schemaVersion)
        {
            CueId = cueId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            Layer = layer;
            Priority = priority;
            Patch = patch;
            FadeInSeconds = Math.Max(0d, fadeInSeconds);
            HoldMode = holdMode;
            DurationSeconds = Math.Max(0d, durationSeconds);
            FadeOutSeconds = Math.Max(0d, fadeOutSeconds);
            ReleasePolicy = releasePolicy;
            FollowCueId = followCueId ?? string.Empty;
            PreconditionId = preconditionId ?? string.Empty;
            SafetyClassification = safetyClassification;
            SchemaVersion = Math.Max(1, schemaVersion);
        }
    }

    public readonly struct FanlightCueCommand
    {
        public string CommandId { get; }
        public string SourceId { get; }
        public FanlightCueCommandType Type { get; }
        public string CueId { get; }
        public string ReplacementCueId { get; }
        public double ShowSeconds { get; }
        public long Sequence { get; }
        public string OperatorId { get; }
        public string SafetyConfirmationId { get; }

        public FanlightCueCommand(
            string commandId,
            string sourceId,
            FanlightCueCommandType type,
            string cueId,
            string replacementCueId,
            double showSeconds,
            long sequence)
            : this(commandId, sourceId, type, cueId, replacementCueId, showSeconds, sequence, sourceId, string.Empty)
        {
        }

        public FanlightCueCommand(
            string commandId,
            string sourceId,
            FanlightCueCommandType type,
            string cueId,
            string replacementCueId,
            double showSeconds,
            long sequence,
            string operatorId,
            string safetyConfirmationId)
        {
            CommandId = commandId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            Type = type;
            CueId = cueId ?? string.Empty;
            ReplacementCueId = replacementCueId ?? string.Empty;
            ShowSeconds = showSeconds;
            Sequence = sequence;
            OperatorId = operatorId ?? string.Empty;
            SafetyConfirmationId = safetyConfirmationId ?? string.Empty;
        }
    }

    public readonly struct FanlightCueRuntimeState
    {
        public string CueId { get; }
        public FanlightCueState State { get; }
        public double StartSeconds { get; }
        public double ReleaseSeconds { get; }
        public float Weight { get; }
        public string FaultCode { get; }

        public FanlightCueRuntimeState(string cueId, FanlightCueState state, double startSeconds, double releaseSeconds, float weight, string faultCode)
        {
            CueId = cueId ?? string.Empty;
            State = state;
            StartSeconds = startSeconds;
            ReleaseSeconds = releaseSeconds;
            Weight = weight;
            FaultCode = faultCode ?? string.Empty;
        }
    }

    public interface IFanlightCueSession
    {
        string SessionId { get; }
        bool IsSafetyStopped { get; }
        bool HasLoggingFault { get; }
        void Submit(in FanlightCueCommand command);
        bool TryGetCueState(string cueId, double showSeconds, out FanlightCueRuntimeState state);
        void CollectContributions(double showSeconds, FanlightContributionBuffer destination);
        FanlightLiveEventLog CaptureEventLog();
    }

    public sealed class FanlightCueSession : IFanlightCueSession
    {
        private readonly Dictionary<string, FanlightCueDefinition> _definitions = new(StringComparer.Ordinal);
        private readonly List<FanlightCueCommand> _commands = new();
        private readonly HashSet<string> _commandIds = new(StringComparer.Ordinal);
        private readonly FanlightMutableEventLog _eventLog;
        private readonly FanlightIntentPatch _safetyPatch;

        public FanlightCueSession(
            string showId,
            string sessionId,
            IEnumerable<FanlightCueDefinition> definitions,
            int eventLogCapacity = FanlightMutableEventLog.DefaultCapacity)
        {
            SessionId = sessionId ?? string.Empty;
            _eventLog = new FanlightMutableEventLog(showId, SessionId, capacity: eventLogCapacity);
            _safetyPatch = FanlightSafetyStateV1.BlackoutPatch;
            if (definitions == null) return;
            foreach (var definition in definitions)
            {
                if (string.IsNullOrEmpty(definition.CueId)
                    || string.IsNullOrEmpty(definition.DisplayName)
                    || string.IsNullOrEmpty(definition.SourceId))
                    throw new ArgumentException("Cue ID, display name, and source ID are required.", nameof(definitions));
                _definitions.Add(definition.CueId, definition);
            }
        }

        public string SessionId { get; }
        public bool IsSafetyStopped { get; private set; }
        public bool HasLoggingFault { get; private set; }

        public void Submit(in FanlightCueCommand command)
        {
            if (string.IsNullOrEmpty(command.CommandId) || string.IsNullOrEmpty(command.SourceId))
                throw new ArgumentException("Command and source IDs are required.", nameof(command));
            if (double.IsNaN(command.ShowSeconds) || double.IsInfinity(command.ShowSeconds))
                throw new ArgumentException("Command show seconds must be finite.", nameof(command));
            if (_commandIds.Contains(command.CommandId)) return;
            if (command.Type is not FanlightCueCommandType.ClearLayer and not FanlightCueCommandType.SafetyStop and not FanlightCueCommandType.Resume
                && !_definitions.ContainsKey(command.CueId))
                throw new InvalidOperationException($"Unknown cue: {command.CueId}");
            if (command.Type == FanlightCueCommandType.Replace && !_definitions.ContainsKey(command.ReplacementCueId))
                throw new InvalidOperationException($"Unknown replacement cue: {command.ReplacementCueId}");
            if (command.Type is FanlightCueCommandType.SafetyStop or FanlightCueCommandType.Resume
                && string.IsNullOrWhiteSpace(command.OperatorId))
                throw new InvalidOperationException("Safety commands require an operator ID.");
            if (command.Type == FanlightCueCommandType.Resume)
            {
                if (!TryGetActiveSafetyStopId(command.ShowSeconds, out var activeStopId))
                    throw new InvalidOperationException("Safety is not stopped at the command time.");
                if (!string.Equals(command.SafetyConfirmationId, activeStopId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Safety resume confirmation does not match the active stop.");
            }
            var liveEvent = ToEvent(command);
            try
            {
                if (!_eventLog.Append(liveEvent)) return;
            }
            catch (InvalidOperationException) when (command.Type == FanlightCueCommandType.SafetyStop)
            {
                HasLoggingFault = true;
            }
            _commandIds.Add(command.CommandId);
            var index = _commands.BinarySearch(command, CommandComparer.Instance);
            if (index < 0) index = ~index;
            _commands.Insert(index, command);
            RecomputeSafetyState();
        }

        public void CollectContributions(double seconds, FanlightContributionBuffer destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            foreach (var pair in _definitions)
            {
                if (!TryResolve(pair.Value, seconds, out var start, out var release, out var aborted)) continue;
                if (aborted && seconds >= release) continue;
                var definition = pair.Value;
                var end = ResolveEnd(definition, start, release);
                var policy = aborted ? FanlightReleasePolicy.RestoreUnderlying : definition.ReleasePolicy;
                destination.Add(new FanlightContribution(
                    $"cue:{definition.CueId}",
                    definition.SourceId,
                    definition.Layer,
                    definition.Priority,
                    start,
                    end,
                    definition.FadeInSeconds,
                    definition.FadeOutSeconds,
                    1f,
                    FanlightBlendProfile.SmoothStep,
                    policy,
                    definition.Patch));
            }

            if (TryGetSafetyState(seconds, out var safetyStart))
            {
                destination.Add(new FanlightContribution(
                    $"safety:{SessionId}",
                    $"safety:{SessionId}",
                    FanlightContributionLayer.Safety,
                    int.MaxValue,
                    safetyStart,
                    double.PositiveInfinity,
                    0d,
                    0d,
                    1f,
                    FanlightBlendProfile.Linear,
                    FanlightReleasePolicy.RestoreUnderlying,
                    _safetyPatch));
            }
        }

        public void Collect(double seconds, FanlightContributionBuffer destination) =>
            CollectContributions(seconds, destination);

        public bool TryGetCueState(string cueId, double showSeconds, out FanlightCueRuntimeState state)
        {
            if (!_definitions.TryGetValue(cueId, out var definition))
            {
                state = new FanlightCueRuntimeState(cueId, FanlightCueState.Faulted, double.NaN, double.NaN, 0f, "CueMissing");
                return false;
            }
            if (!TryResolve(definition, showSeconds, out var start, out var release, out var aborted))
            {
                var idleState = IsArmed(cueId, showSeconds) ? FanlightCueState.Armed : FanlightCueState.Idle;
                state = new FanlightCueRuntimeState(cueId, idleState, double.NaN, double.NaN, 0f, string.Empty);
                return true;
            }
            if (aborted)
            {
                state = new FanlightCueRuntimeState(cueId, FanlightCueState.Aborted, start, release, 0f, string.Empty);
                return true;
            }
            var end = ResolveEnd(definition, start, release);
            var weight = EvaluateWeight(definition, start, end, showSeconds);
            var cueState = showSeconds > end ? FanlightCueState.Completed
                : showSeconds < start + definition.FadeInSeconds ? FanlightCueState.FadingIn
                : showSeconds > end - definition.FadeOutSeconds ? FanlightCueState.FadingOut
                : FanlightCueState.Active;
            state = new FanlightCueRuntimeState(cueId, cueState, start, release, weight, string.Empty);
            return true;
        }

        public FanlightLiveEventLog CaptureEventLog() => _eventLog.Capture();

        private bool TryResolve(FanlightCueDefinition definition, double seconds, out double start, out double release, out bool aborted)
        {
            start = double.NaN;
            release = double.NaN;
            aborted = false;
            for (var i = 0; i < _commands.Count; i++)
            {
                var command = _commands[i];
                if (command.ShowSeconds > seconds) break;
                var startsThisCue = command.Type == FanlightCueCommandType.Go && command.CueId == definition.CueId;
                var replacementStarts = command.Type == FanlightCueCommandType.Replace && command.ReplacementCueId == definition.CueId;
                if (startsThisCue || replacementStarts)
                {
                    start = command.ShowSeconds;
                    release = double.NaN;
                    aborted = false;
                }
                if (double.IsNaN(start)) continue;
                if (command.Type == FanlightCueCommandType.ClearLayer)
                {
                    release = command.ShowSeconds;
                    aborted = true;
                    continue;
                }
                if (command.CueId != definition.CueId) continue;
                if (command.Type is FanlightCueCommandType.Release or FanlightCueCommandType.Replace)
                    release = command.ShowSeconds;
                if (command.Type == FanlightCueCommandType.Abort)
                {
                    release = command.ShowSeconds;
                    aborted = true;
                }
            }
            return !double.IsNaN(start);
        }

        private bool IsArmed(string cueId, double seconds)
        {
            var armed = false;
            for (var i = 0; i < _commands.Count; i++)
            {
                var command = _commands[i];
                if (command.ShowSeconds > seconds) break;
                if (command.Type == FanlightCueCommandType.ClearLayer)
                {
                    armed = false;
                    continue;
                }
                if (command.Type == FanlightCueCommandType.Arm && command.CueId == cueId) armed = true;
                if ((command.Type == FanlightCueCommandType.Go && command.CueId == cueId)
                    || (command.Type == FanlightCueCommandType.Replace && command.ReplacementCueId == cueId)
                    || (command.Type == FanlightCueCommandType.Abort && command.CueId == cueId))
                    armed = false;
            }
            return armed;
        }

        private bool TryGetSafetyState(double seconds, out double startSeconds)
        {
            var active = false;
            startSeconds = double.NaN;
            for (var i = 0; i < _commands.Count; i++)
            {
                var command = _commands[i];
                if (command.ShowSeconds > seconds) break;
                if (command.Type == FanlightCueCommandType.SafetyStop)
                {
                    active = true;
                    startSeconds = command.ShowSeconds;
                }
                else if (command.Type == FanlightCueCommandType.Resume)
                {
                    active = false;
                    startSeconds = double.NaN;
                }
            }
            return active;
        }

        private void RecomputeSafetyState()
        {
            IsSafetyStopped = false;
            for (var i = 0; i < _commands.Count; i++)
            {
                if (_commands[i].Type == FanlightCueCommandType.SafetyStop) IsSafetyStopped = true;
                if (_commands[i].Type == FanlightCueCommandType.Resume) IsSafetyStopped = false;
            }
        }

        private bool TryGetActiveSafetyStopId(double seconds, out string commandId)
        {
            commandId = string.Empty;
            for (var i = 0; i < _commands.Count; i++)
            {
                var command = _commands[i];
                if (command.ShowSeconds > seconds) break;
                if (command.Type == FanlightCueCommandType.SafetyStop) commandId = command.CommandId;
                else if (command.Type == FanlightCueCommandType.Resume) commandId = string.Empty;
            }
            return commandId.Length > 0;
        }

        private static double ResolveEnd(FanlightCueDefinition definition, double start, double release)
        {
            if (!double.IsNaN(release)) return release + definition.FadeOutSeconds;
            if (definition.HoldMode == FanlightCueHoldMode.Timed)
                return start + definition.DurationSeconds + definition.FadeOutSeconds;
            return double.PositiveInfinity;
        }

        private static float EvaluateWeight(FanlightCueDefinition definition, double start, double end, double seconds)
        {
            if (seconds < start || seconds > end) return 0f;
            var weight = 1d;
            if (definition.FadeInSeconds > 0d && seconds < start + definition.FadeInSeconds)
                weight = (seconds - start) / definition.FadeInSeconds;
            if (definition.FadeOutSeconds > 0d && seconds > end - definition.FadeOutSeconds)
                weight = Math.Min(weight, (end - seconds) / definition.FadeOutSeconds);
            return (float)Math.Max(0d, Math.Min(1d, weight));
        }

        private static FanlightLiveEvent ToEvent(FanlightCueCommand command)
        {
            var type = command.Type switch
            {
                FanlightCueCommandType.Arm => FanlightLiveEventType.CueArmed,
                FanlightCueCommandType.Go => FanlightLiveEventType.CueStarted,
                FanlightCueCommandType.Release => FanlightLiveEventType.CueReleased,
                FanlightCueCommandType.Abort => FanlightLiveEventType.CueAborted,
                FanlightCueCommandType.Replace => FanlightLiveEventType.CueReplaced,
                FanlightCueCommandType.ClearLayer => FanlightLiveEventType.LayerCleared,
                FanlightCueCommandType.SafetyStop => FanlightLiveEventType.SafetyStopped,
                _ => FanlightLiveEventType.SafetyResumed
            };
            return new FanlightLiveEvent(command.CommandId, command.SourceId, type, command.ShowSeconds, command.Sequence,
                command.CueId, command.ReplacementCueId, 0, default, false, string.Empty, 1, command.OperatorId);
        }

        private sealed class CommandComparer : IComparer<FanlightCueCommand>
        {
            public static readonly CommandComparer Instance = new();
            public int Compare(FanlightCueCommand left, FanlightCueCommand right)
            {
                var time = left.ShowSeconds.CompareTo(right.ShowSeconds);
                if (time != 0) return time;
                var sequence = left.Sequence.CompareTo(right.Sequence);
                return sequence != 0 ? sequence : string.Compare(left.CommandId, right.CommandId, StringComparison.Ordinal);
            }
        }
    }

    public static class FanlightSafetyStateV1
    {
        public static FanlightIntentPatch BlackoutPatch { get; } = CreateBlackoutPatch();

        private static FanlightIntentPatch CreateBlackoutPatch()
        {
            var black = new FanlightPaletteIntent(
                Color.black, Color.black, Color.black, Color.black, Color.black, Color.black, 0f, 0f);
            return new FanlightIntentPatchBuilder()
                .SetGesture("Hold")
                .SetEnergy(0f)
                .SetParticipation(0f)
                .SetSynchronization(1f)
                .SetRealism(0f)
                .SetReach(0f)
                .SetPalette(new FanlightPalettePatch(black, FanlightPaletteFieldMask.All))
                .SetPenlightsEnabled(false)
                .SetAudienceBodiesEnabled(false)
                .Build();
        }
    }
}
