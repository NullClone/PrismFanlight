using System;
using System.Collections.Generic;

namespace PrismFanlight.Live
{
    internal sealed class FanlightShowEventLog
    {
        internal const int FormatVersion = 1;

        private const int DefaultCapacity = 32;

        private readonly List<FanlightShowEventLogEntry> _entries;
        private readonly HashSet<string> _stableEventIds;
        private ulong _lastSequence;

        internal int Count => _entries.Count;

        internal FanlightShowEventLog(int initialCapacity = DefaultCapacity)
        {
            if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            _entries = new List<FanlightShowEventLogEntry>(initialCapacity);
            _stableEventIds = new HashSet<string>(initialCapacity, StringComparer.Ordinal);
        }

        internal FanlightShowEventLog(ReadOnlySpan<FanlightShowEventLogEntry> entries)
            : this(entries.Length)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.Sequence <= _lastSequence)
                    throw new ArgumentException("Event sequences must be strictly increasing.", nameof(entries));
                AddExisting(entry);
            }
        }

        internal FanlightShowEventLogEntry Append(
            double showSeconds,
            string stableEventId,
            FanlightShowCommand command)
        {
            if (_lastSequence == ulong.MaxValue) throw new InvalidOperationException("Event sequence exhausted.");
            var entry = new FanlightShowEventLogEntry(
                _lastSequence + 1UL,
                showSeconds,
                stableEventId,
                command);
            AddExisting(entry);
            return entry;
        }

        internal FanlightShowEventLogEntry GetAt(int index) => _entries[index];

        private void AddExisting(FanlightShowEventLogEntry entry)
        {
            if (!_stableEventIds.Add(entry.StableEventId))
                throw new InvalidOperationException($"Duplicate stable event ID: {entry.StableEventId}");
            var index = _entries.BinarySearch(entry, EntryComparer.Instance);
            if (index < 0) index = ~index;
            _entries.Insert(index, entry);
            _lastSequence = entry.Sequence;
        }

        private sealed class EntryComparer : IComparer<FanlightShowEventLogEntry>
        {
            internal static readonly EntryComparer Instance = new();

            public int Compare(FanlightShowEventLogEntry left, FanlightShowEventLogEntry right)
            {
                var seconds = left.ShowSeconds.CompareTo(right.ShowSeconds);
                if (seconds != 0) return seconds;
                var sequence = left.Sequence.CompareTo(right.Sequence);
                if (sequence != 0) return sequence;
                return string.Compare(left.StableEventId, right.StableEventId, StringComparison.Ordinal);
            }
        }
    }
}
