using PrismFanlight.Time;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTempoMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private PrismFanlight _lastTarget;
        private PlayableDirector _director;
        private TrackAsset _track;
        private FanlightTempoSource _source;
        private FanlightTimeManager _definitionTimeManager;
        private int _definitionTempoRevision = int.MinValue;


        // Properties

        internal FanlightTempoRuntimeDefinition Definition { get; private set; }


        // Methods

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as PrismFanlight;

            if (_lastTarget != target)
            {
                ChangeTarget(target);
            }

            if (target == null) return;

            target.MarkScheduledTimelineEvaluation();

            if (!TryEnsureDefinition(target, out var hasClips) || !hasClips)
            {
                target.ClearScheduledTempoCandidate(this);
                return;
            }

            var sequencePlayable = playable.GetGraph().GetRootPlayable(0);

            target.SetScheduledTempoCandidate(this, new FanlightTempoCandidate(sequencePlayable.GetTime(), Definition));
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledTempoCandidate(this);
                if (!IsCurrentBinding()) _lastTarget.ClearHeldTimelineState();
            }

            _lastTarget = null;
        }

        internal void Configure(
            FanlightTempoSource source,
            PlayableDirector director,
            TrackAsset track)
        {
            _source = source;
            _director = director;
            _track = track;
        }

        private void ChangeTarget(PrismFanlight target)
        {
            if (_lastTarget != null)
            {
                _lastTarget.ClearScheduledTempoCandidate(this);
                _lastTarget.ClearHeldTimelineState();
            }

            _lastTarget = target;
            Definition = null;
            _source = null;
            _definitionTimeManager = null;
            _definitionTempoRevision = int.MinValue;
        }

        private bool TryEnsureDefinition(PrismFanlight target, out bool hasClips)
        {
            hasClips = false;
            var timeManager = target.TimeManager;

            if (timeManager == null)
            {
                target.ReportTimelineFault("Tempo Track requires a bound PrismFanlight with a Fanlight Time Manager.");
                Definition = null;
                _source = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                return false;
            }

            if (_track is not FanlightTempoTrack tempoTrack)
            {
                target.ReportTimelineFault("Tempo Track is not configured correctly.");
                Definition = null;
                _source = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                return false;
            }

            if (!tempoTrack.TryBuildTempoSource(out var source, out var sourceError))
            {
                target.ReportTimelineFault(sourceError);
                Definition = null;
                _source = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                return false;
            }

            if (!source.HasClips)
            {
                Definition = null;
                _source = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                return true;
            }

            hasClips = true;

            if (Definition != null
                && _definitionTimeManager == timeManager
                && _definitionTempoRevision == timeManager.DefaultTempoRevision
                && SourcesEqual(_source, source))
            {
                return true;
            }

            if (!FanlightTempoDefinitionBuilder.TryBuildDefinition(
                    source,
                    timeManager.DefaultBpm,
                    out var definition,
                    out var error))
            {
                target.ReportTimelineFault(error);
                Definition = null;
                _source = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                hasClips = false;
                return false;
            }

            Definition = definition;
            _source = source;
            _definitionTimeManager = timeManager;
            _definitionTempoRevision = timeManager.DefaultTempoRevision;
            return true;
        }

        private static bool SourcesEqual(FanlightTempoSource a, FanlightTempoSource b)
        {
            if (a == null || b == null) return false;

            if (a.BeatsPerBar != b.BeatsPerBar || a.BeatUnit != b.BeatUnit) return false;

            var aStarts = a.Starts.Span;
            var bStarts = b.Starts.Span;

            if (aStarts.Length != bStarts.Length) return false;

            var aEnds = a.Ends.Span;
            var bEnds = b.Ends.Span;
            var aBpms = a.Bpms.Span;
            var bBpms = b.Bpms.Span;

            for (var i = 0; i < aStarts.Length; i++)
            {
                if (aStarts[i] != bStarts[i] || aEnds[i] != bEnds[i] || aBpms[i] != bBpms[i]) return false;
            }

            return true;
        }

        private bool IsCurrentBinding()
        {
            if (_director == null || _track == null || _lastTarget == null) return false;

            var binding = _director.GetGenericBinding(_track);
            if (binding == _lastTarget) return true;
            return binding is GameObject gameObject && gameObject.GetComponent<PrismFanlight>() == _lastTarget;
        }
    }
}
