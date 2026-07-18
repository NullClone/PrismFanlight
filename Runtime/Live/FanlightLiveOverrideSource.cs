using System;
using System.Collections.Generic;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    internal sealed class FanlightLiveOverrideSource : IFanlightContributionSource
    {
        private readonly FanlightMutableEventLog _eventLog;
        private readonly Dictionary<string, FanlightLiveEvent> _active = new(StringComparer.Ordinal);
        private long _eventSequence;

        internal string SourceId { get; }
        internal int Priority { get; }

        internal FanlightLiveOverrideSource(FanlightMutableEventLog eventLog, string sourceId = "live.override", int priority = 0)
        {
            _eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? "live.override" : sourceId;
            Priority = priority;
        }

        internal void CreateOrUpdate(string overrideId, double showSeconds, FanlightShowPatch patch, bool update = false)
        {
            if (string.IsNullOrWhiteSpace(overrideId)) throw new ArgumentException("Override ID is required.", nameof(overrideId));
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

        internal void Release(string overrideId, double showSeconds)
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
                if (!string.Equals(value.SourceId, SourceId, StringComparison.Ordinal)) continue;
                if (value.EventType == FanlightLiveEventType.LiveOverrideReleased)
                    _active.Remove(value.TargetId);
                else if (value.EventType is FanlightLiveEventType.LiveOverrideCreated or FanlightLiveEventType.LiveOverrideUpdated)
                    _active[value.TargetId] = value;
            }

            foreach (var pair in _active)
            {
                var value = pair.Value;
                destination.Add(new FanlightShowContribution(
                    $"{SourceId}:{pair.Key}",
                    FanlightContributionLayer.Live,
                    Priority,
                    value.ShowSeconds,
                    double.PositiveInfinity,
                    1f,
                    value.Patch));
            }
        }
    }
}
