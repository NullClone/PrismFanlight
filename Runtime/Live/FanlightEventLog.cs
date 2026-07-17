using System;
using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    public sealed class FanlightMutableEventLog : FanlightLiveEventLog
    {
        public const int DefaultCapacity = 100000;
        private readonly List<FanlightLiveEvent> _events = new();
        private readonly HashSet<string> _eventIds = new(StringComparer.Ordinal);
        private readonly int _capacity;

        public FanlightMutableEventLog(string showId, string sessionId, int schemaVersion = 1, int capacity = DefaultCapacity)
        {
            ShowId = showId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            SchemaVersion = Math.Max(1, schemaVersion);
            _capacity = Math.Max(1, capacity);
        }

        public override string ShowId { get; }
        public override string SessionId { get; }
        public override int SchemaVersion { get; }
        public override int Count => _events.Count;
        public int Capacity => _capacity;
        public bool IsFull => _events.Count >= _capacity;
        public override FanlightLiveEvent GetAt(int index) => _events[index];

        public bool Append(in FanlightLiveEvent value)
        {
            if (string.IsNullOrEmpty(value.EventId)) throw new ArgumentException("Event ID is required.", nameof(value));
            if (_eventIds.Contains(value.EventId)) return false;
            if (IsFull) throw new InvalidOperationException("EventLogCapacityExceeded");
            _eventIds.Add(value.EventId);
            var index = _events.BinarySearch(value, EventComparer.Instance);
            if (index < 0) index = ~index;
            _events.Insert(index, value);
            return true;
        }

        public FanlightLiveEventLogSnapshot Capture() =>
            new(ShowId, SessionId, SchemaVersion, _events.ToArray());

        private sealed class EventComparer : IComparer<FanlightLiveEvent>
        {
            public static readonly EventComparer Instance = new();
            public int Compare(FanlightLiveEvent left, FanlightLiveEvent right)
            {
                var time = left.ShowSeconds.CompareTo(right.ShowSeconds);
                if (time != 0) return time;
                var sequence = left.Sequence.CompareTo(right.Sequence);
                return sequence != 0 ? sequence : string.Compare(left.EventId, right.EventId, StringComparison.Ordinal);
            }
        }
    }

    public sealed class FanlightTimeEventRecorder
    {
        private readonly FanlightMutableEventLog _log;
        private readonly string _sourceId;
        private long _lastSequence = long.MinValue;
        private string _lastProviderId = string.Empty;
        private bool _lastFallback;

        public FanlightTimeEventRecorder(FanlightMutableEventLog log, string sourceId = "time.coordinator")
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _sourceId = sourceId ?? "time.coordinator";
        }

        public void Observe(in FanlightShowTimeSample sample)
        {
            if (sample.Sequence <= _lastSequence) return;
            if (sample.IsFallbackActive && !_lastFallback)
            {
                _log.Append(new FanlightLiveEvent(
                    $"time-disconnected:{sample.TimeDomainId}:{sample.Sequence}",
                    _sourceId,
                    FanlightLiveEventType.ClockDisconnected,
                    sample.Seconds,
                    sample.Sequence,
                    sample.ProviderId,
                    _lastProviderId,
                    sample.TimeDomainVersion,
                    default,
                    false,
                    "FallbackActivated",
                    1));
            }
            var type = sample.Discontinuity switch
            {
                FanlightTimeDiscontinuity.Reconnected => FanlightLiveEventType.ClockReconnected,
                FanlightTimeDiscontinuity.AuthorityChanged => FanlightLiveEventType.ClockAuthorityChanged,
                _ => (FanlightLiveEventType)(-1)
            };
            if ((int)type >= 0)
            {
                var note = sample.IsFallbackActive && !_lastFallback
                    ? "FallbackActivated"
                    : !sample.IsFallbackActive && _lastFallback
                        ? "PrimaryReacquired"
                        : sample.Discontinuity.ToString();
                _log.Append(new FanlightLiveEvent(
                    $"time:{sample.TimeDomainId}:{sample.Sequence}",
                    _sourceId,
                    type,
                    sample.Seconds,
                    sample.Sequence,
                    sample.ProviderId,
                    _lastProviderId,
                    sample.TimeDomainVersion,
                    default,
                    false,
                    note,
                    1));
            }
            _lastSequence = sample.Sequence;
            _lastProviderId = sample.ProviderId;
            _lastFallback = sample.IsFallbackActive;
        }
    }
}
