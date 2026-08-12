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

            if (_source == null || !_source.HasClips)
            {
                target.ClearScheduledTempoCandidate(this);
                return;
            }

            if (!TryEnsureDefinition(target)) return;

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
            _definitionTimeManager = null;
            _definitionTempoRevision = int.MinValue;
        }

        private bool TryEnsureDefinition(PrismFanlight target)
        {
            var timeManager = target.TimeManager;

            if (timeManager == null)
            {
                target.ClearScheduledTempoCandidate(this);
                target.ReportTimelineFault("Tempo Track requires a bound PrismFanlight with a Fanlight Time Manager.");
                Definition = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                return false;
            }

            if (Definition != null
                && _definitionTimeManager == timeManager
                && _definitionTempoRevision == timeManager.DefaultTempoRevision)
            {
                return true;
            }

            if (!FanlightTempoDefinitionBuilder.TryBuildDefinition(
                    _source,
                    timeManager.DefaultBpm,
                    out var definition,
                    out var error))
            {
                target.ClearScheduledTempoCandidate(this);
                target.ReportTimelineFault(error);
                Definition = null;
                _definitionTimeManager = null;
                _definitionTempoRevision = int.MinValue;
                return false;
            }

            Definition = definition;
            _definitionTimeManager = timeManager;
            _definitionTempoRevision = timeManager.DefaultTempoRevision;
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
