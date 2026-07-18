using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [CreateAssetMenu(fileName = "FanlightTempoMap", menuName = "Prism Fanlight/Tempo Map")]
    public sealed class FanlightTempoMap : ScriptableObject
    {
        // Fields

        [SerializeField]
        private string _tempoMapId = string.Empty;

        [SerializeField, Min(1)]
        private int _version = 1;

        [SerializeField]
        private FanlightTempoSegment[] _segments =
        {
            new("tempo.default", 0d, 0d, 120d, 4, 4, 1)
        };


        // Properties

        public string TempoMapId => _tempoMapId ?? string.Empty;

        public int Version => Math.Max(1, _version);

        public ReadOnlyMemory<FanlightTempoSegment> Segments => _segments ?? Array.Empty<FanlightTempoSegment>();


        // Methods

        public bool Validate(out string error)
        {
            if (string.IsNullOrEmpty(TempoMapId))
            {
                error = "Tempo Map ID is missing.";
                return false;
            }

            if (_segments == null || _segments.Length == 0)
            {
                error = "At least one tempo segment is required.";
                return false;
            }

            for (var i = 0; i < _segments.Length; i++)
            {
                if (!_segments[i].IsValid)
                {
                    error = $"Tempo segment {i} is invalid.";
                    return false;
                }

                if (i > 0 && _segments[i].StartSeconds <= _segments[i - 1].StartSeconds)
                {
                    error = "Tempo segments must be strictly ordered by start seconds.";
                    return false;
                }

                if (i > 0)
                {
                    var previous = _segments[i - 1];
                    var expectedBeat = previous.StartBeat
                                       + (_segments[i].StartSeconds - previous.StartSeconds) * previous.Bpm / 60d;
                    if (Math.Abs(_segments[i].StartBeat - expectedBeat) > 1e-6d)
                    {
                        error = $"Tempo segment {i} must continue the previous segment beat.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _version = Math.Max(1, _version);
            if (string.IsNullOrEmpty(_tempoMapId)) _tempoMapId = Guid.NewGuid().ToString("N");
        }
#endif
    }
}
