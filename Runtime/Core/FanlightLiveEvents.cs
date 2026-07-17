using System;

namespace PrismFanlight.Core
{
    public enum FanlightLiveEventType
    {
        ClockAuthorityChanged = 0,
        ClockDisconnected = 1,
        ClockReconnected = 2,
        CueArmed = 10,
        CueStarted = 11,
        CueReleased = 12,
        CueAborted = 13,
        CueReplaced = 14,
        LayerCleared = 15,
        LiveOverrideCreated = 20,
        LiveOverrideUpdated = 21,
        LiveOverrideReleased = 22,
        SafetyStopped = 30,
        SafetyResumed = 31,
        ShowSnapshotChanged = 40,
        LayoutChanged = 41,
        PersonaProfileChanged = 42,
        OperatorNote = 50
    }

    public readonly struct FanlightLiveEvent
    {
        public string EventId { get; }
        public string SourceId { get; }
        public FanlightLiveEventType EventType { get; }
        public double ShowSeconds { get; }
        public long Sequence { get; }
        public string TargetId { get; }
        public string SecondaryTargetId { get; }
        public int TargetVersion { get; }
        public FanlightIntentPatch Patch { get; }
        public bool HasPatch { get; }
        public string Note { get; }
        public string OperatorId { get; }
        public int SchemaVersion { get; }

        public FanlightLiveEvent(
            string eventId,
            string sourceId,
            FanlightLiveEventType eventType,
            double showSeconds,
            long sequence,
            string targetId,
            string secondaryTargetId,
            int targetVersion,
            FanlightIntentPatch patch,
            bool hasPatch,
            string note,
            int schemaVersion,
            string operatorId = "system")
        {
            EventId = eventId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            EventType = eventType;
            ShowSeconds = showSeconds;
            Sequence = sequence;
            TargetId = targetId ?? string.Empty;
            SecondaryTargetId = secondaryTargetId ?? string.Empty;
            TargetVersion = targetVersion;
            Patch = patch;
            HasPatch = hasPatch;
            Note = note ?? string.Empty;
            OperatorId = string.IsNullOrWhiteSpace(operatorId) ? "system" : operatorId;
            SchemaVersion = schemaVersion;
        }
    }

    public abstract class FanlightLiveEventLog
    {
        public abstract string ShowId { get; }
        public abstract string SessionId { get; }
        public abstract int SchemaVersion { get; }
        public abstract int Count { get; }
        public abstract FanlightLiveEvent GetAt(int index);

        public static FanlightLiveEventLog Empty { get; } =
            new FanlightLiveEventLogSnapshot(string.Empty, string.Empty, 1, Array.Empty<FanlightLiveEvent>());
    }

    public sealed class FanlightLiveEventLogSnapshot : FanlightLiveEventLog
    {
        private readonly FanlightLiveEvent[] _events;

        public FanlightLiveEventLogSnapshot(string showId, string sessionId, int schemaVersion, FanlightLiveEvent[] events)
        {
            ShowId = showId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            SchemaVersion = schemaVersion;
            _events = events == null ? Array.Empty<FanlightLiveEvent>() : (FanlightLiveEvent[])events.Clone();
            Array.Sort(_events, Compare);
        }

        public override string ShowId { get; }
        public override string SessionId { get; }
        public override int SchemaVersion { get; }
        public override int Count => _events.Length;
        public override FanlightLiveEvent GetAt(int index) => _events[index];

        private static int Compare(FanlightLiveEvent left, FanlightLiveEvent right)
        {
            var time = left.ShowSeconds.CompareTo(right.ShowSeconds);
            if (time != 0) return time;
            var sequence = left.Sequence.CompareTo(right.Sequence);
            return sequence != 0 ? sequence : string.Compare(left.EventId, right.EventId, StringComparison.Ordinal);
        }
    }
}
