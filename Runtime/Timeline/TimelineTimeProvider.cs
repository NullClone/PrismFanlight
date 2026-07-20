using System;
using PrismFanlight.Core;
using PrismFanlight.Time;
using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    [AddComponentMenu("Prism Fanlight/Time Providers/Timeline Time Provider")]
    public sealed class TimelineTimeProvider : MonoBehaviour, IShowTimeProvider
    {
        // Fields

        [SerializeField]
        private PlayableDirector _director;

        [SerializeField, Min(1e-9f)]
        private double _seekTolerance = 1e-5d;


        private bool _hasPrevious;
        private double _previousSeconds;
        private double _previousClockSeconds;
        private double _previousRate;
        private DirectorUpdateMode _previousUpdateMode;


        // Properties

        private bool IsConfigured => _director != null && _director.playableAsset != null;


        // Methods

        ShowTimeProviderSample IShowTimeProvider.Sample()
        {
            if (!IsConfigured)
            {
                return new ShowTimeProviderSample(
                    _hasPrevious ? _previousSeconds : 0d,
                    0d,
                    FanlightClockStatus.Disconnected,
                    FanlightTimeDiscontinuity.None);
            }

            var seconds = _director.time;
            var updateMode = _director.timeUpdateMode;
            var clockSeconds = GetClockSeconds(updateMode);
            var rate = _director.state == PlayState.Playing ? GetRate() : 0d;
            var discontinuity = FanlightTimeDiscontinuity.None;

            if (_hasPrevious)
            {
                var actual = seconds - _previousSeconds;
                var expected = (clockSeconds - _previousClockSeconds) * _previousRate;
                if (updateMode != _previousUpdateMode)
                {
                    discontinuity = FanlightTimeDiscontinuity.Seek;
                }
                else if (rate < 0d && _previousRate >= 0d)
                {
                    discontinuity = FanlightTimeDiscontinuity.Reverse;
                }
                else if (IsForwardLoop(actual, _previousRate))
                {
                    discontinuity = FanlightTimeDiscontinuity.Loop;
                }
                else if (Math.Abs(actual - expected) > _seekTolerance)
                {
                    discontinuity = FanlightTimeDiscontinuity.Seek;
                }
            }

            _hasPrevious = true;
            _previousSeconds = seconds;
            _previousClockSeconds = clockSeconds;
            _previousRate = rate;
            _previousUpdateMode = updateMode;

            var status = rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready;

            return new ShowTimeProviderSample(seconds, rate, status, discontinuity);
        }

        private double GetRate()
        {
            var graph = _director.playableGraph;

            if (!graph.IsValid() || graph.GetRootPlayableCount() == 0)
            {
                return _director.state == PlayState.Playing ? 1d : 0d;
            }

            return graph.GetRootPlayable(0).GetSpeed();
        }

        private static double GetClockSeconds(DirectorUpdateMode updateMode) => updateMode switch
        {
            DirectorUpdateMode.GameTime => UnityEngine.Time.timeAsDouble,
            DirectorUpdateMode.UnscaledGameTime => UnityEngine.Time.unscaledTimeAsDouble,
            DirectorUpdateMode.DSPClock => AudioSettings.dspTime,
            DirectorUpdateMode.Manual => UnityEngine.Time.unscaledTimeAsDouble,
            _ => UnityEngine.Time.unscaledTimeAsDouble
        };

        private bool IsForwardLoop(double actualDelta, double rate) =>
            rate >= 0d
            && actualDelta < -_seekTolerance
            && _director.extrapolationMode == DirectorWrapMode.Loop
            && _previousSeconds >= Math.Max(0d, _director.duration - Math.Max(_seekTolerance, 0.1d));


#if UNITY_EDITOR
        private void OnValidate()
        {
            _seekTolerance = Math.Max(1e-9d, _seekTolerance);
        }
#endif
    }
}
