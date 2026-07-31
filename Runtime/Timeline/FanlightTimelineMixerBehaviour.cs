using System;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    internal sealed class FanlightTimelineMixerBehaviour : PlayableBehaviour
    {
        // Fields

        private PrismFanlight _lastTarget;
        private FanlightTimelinePatchKind _patchKind;
        private FanlightTimelineFieldMask _fieldMask;
        private int _trackPriority;
        private int _trackOrder;
        private PlayableDirector _director;
        private TrackAsset _track;
        private bool _contextAcquired;
        private double[] _clipStartSeconds = Array.Empty<double>();
        private FanlightTimelineClipSample[] _samples = Array.Empty<FanlightTimelineClipSample>();


        // Methods

        public override void OnGraphStart(Playable playable) => AcquireContext();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as PrismFanlight;

            if (_lastTarget != target)
            {
                ChangeTarget(target);
            }

            if (target == null) return;

            target.CancelTimelineRelease(this);
            AcquireContext();
            target.MarkScheduledTimelineEvaluation();

            if (!FanlightTimelinePatchMixer.HasFields(_patchKind, _fieldMask))
            {
                target.ClearScheduledContribution(this);
                return;
            }

            var inputCount = playable.GetInputCount();

            if (_clipStartSeconds.Length != inputCount)
            {
                target.ClearScheduledContribution(this);
                throw new InvalidOperationException("Timeline clip start metadata does not match the playable inputs.");
            }

            EnsureSampleCapacity(2);

            var sampleCount = 0;
            var totalWeight = 0f;

            try
            {
                for (var i = 0; i < inputCount; i++)
                {
                    var timelineWeight = playable.GetInputWeight(i);

                    if (float.IsNaN(timelineWeight) || float.IsInfinity(timelineWeight) || timelineWeight <= 0f) continue;

                    if (sampleCount == 2)
                    {
                        target.ClearScheduledContribution(this);
                        throw new InvalidOperationException("A Prism Fanlight Timeline track cannot evaluate more than two clips at once.");
                    }

                    var input = (ScriptPlayable<FanlightTimelinePlayableBehaviour>)playable.GetInput(i);
                    var behaviour = input.GetBehaviour();

                    _samples[sampleCount++] = new FanlightTimelineClipSample(
                        _clipStartSeconds[i],
                        behaviour.Value,
                        timelineWeight);

                    totalWeight += timelineWeight;
                }

                if (sampleCount == 0)
                {
                    target.ClearScheduledContribution(this);
                    return;
                }

                if (sampleCount == 2 && _samples[0].StartSeconds > _samples[1].StartSeconds)
                {
                    (_samples[0], _samples[1]) = (_samples[1], _samples[0]);
                }

                if (!FanlightTimelinePatchMixer.TryBlend(
                        _patchKind,
                        _fieldMask,
                        _samples.AsSpan(0, sampleCount),
                        out var patch))
                {
                    target.ClearScheduledContribution(this);
                    return;
                }

                target.SetScheduledContribution(
                    this,
                    new FanlightShowContribution(
                        FanlightSequenceContextRegistry.GetContext(_director),
                        _trackPriority,
                        _trackOrder,
                        double.MinValue,
                        double.MaxValue,
                        Mathf.Clamp01(totalWeight),
                        patch));
            }
            finally
            {
                ClearSamples(sampleCount);
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_lastTarget == null)
            {
                ReleaseContext();
                return;
            }

            if (IsCurrentBinding())
            {
                _lastTarget.ScheduleTimelineRelease(this, ReleaseContext);
            }
            else
            {
                _lastTarget.CancelTimelineRelease(this);
                _lastTarget.ClearScheduledContribution(this);
                _lastTarget.ClearHeldTimelineState();
                ReleaseContext();
            }

            _lastTarget = null;
        }

        internal void Configure(
            FanlightTimelinePatchKind patchKind,
            FanlightTimelineFieldMask fieldMask,
            int trackPriority,
            int trackOrder,
            double[] clipStartSeconds,
            PlayableDirector director,
            TrackAsset track)
        {
            _patchKind = patchKind;
            _fieldMask = fieldMask;
            _trackPriority = trackPriority;
            _trackOrder = trackOrder;
            _clipStartSeconds = clipStartSeconds ?? Array.Empty<double>();
            _director = director;
            _track = track;
        }

        private void EnsureSampleCapacity(int capacity)
        {
            if (_samples.Length >= capacity) return;
            Array.Resize(ref _samples, capacity);
        }

        private void ClearSamples(int count)
        {
            if (count > 0) Array.Clear(_samples, 0, count);
        }

        private void ChangeTarget(PrismFanlight target)
        {
            if (_lastTarget != null)
            {
                _lastTarget.CancelTimelineRelease(this);
                _lastTarget.ClearScheduledContribution(this);
                _lastTarget.ClearHeldTimelineState();
            }

            ReleaseContext();
            _lastTarget = target;
        }

        private void AcquireContext()
        {
            if (_contextAcquired) return;

            FanlightSequenceContextRegistry.Acquire(_director);
            _contextAcquired = true;
        }

        private void ReleaseContext()
        {
            if (!_contextAcquired) return;

            FanlightSequenceContextRegistry.Release(_director);
            _contextAcquired = false;
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
