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
        private string _providerId = string.Empty;

        [SerializeField]
        private PlayableDirector _director;

        [SerializeField, Min(1e-9f)]
        private double _seekTolerance = 1e-5d;


        private bool _hasPrevious;
        private double _previousSeconds;
        private double _previousUnitySeconds;
        private double _previousRate;
        private long _sequence;


        // Properties

        public string ProviderId => _providerId ?? string.Empty;

        public bool IsConfigured => _director != null && _director.playableAsset != null;


        // Methods

        public ShowTimeProviderSample Sample()
        {
            if (!IsConfigured)
            {
                return new ShowTimeProviderSample(
                    ProviderId,
                    _hasPrevious ? _previousSeconds : 0d,
                    0d,
                    FanlightClockStatus.Disconnected,
                    FanlightTimeDiscontinuity.None,
                    ++_sequence);
            }

            var seconds = _director.time;
            var unitySeconds = UnityEngine.Time.unscaledTimeAsDouble;
            var rate = _director.state == PlayState.Playing ? GetRate() : 0d;
            var discontinuity = FanlightTimeDiscontinuity.None;

            if (_hasPrevious)
            {
                var actual = seconds - _previousSeconds;
                var expected = (unitySeconds - _previousUnitySeconds) * rate;
                if (rate < 0d && _previousRate >= 0d)
                {
                    discontinuity = FanlightTimeDiscontinuity.Reverse;
                }
                else if (IsForwardLoop(actual, rate))
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
            _previousUnitySeconds = unitySeconds;
            _previousRate = rate;

            var status = rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready;

            return new ShowTimeProviderSample(ProviderId, seconds, rate, status, discontinuity, ++_sequence);
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

        private bool IsForwardLoop(double actualDelta, double rate) =>
            rate >= 0d
            && actualDelta < -_seekTolerance
            && _director.extrapolationMode == DirectorWrapMode.Loop
            && _previousSeconds >= Math.Max(0d, _director.duration - Math.Max(_seekTolerance, 0.1d));


#if UNITY_EDITOR
        private void OnValidate()
        {
            _seekTolerance = Math.Max(1e-9d, _seekTolerance);

            if (string.IsNullOrEmpty(_providerId)) _providerId = $"timeline.{Guid.NewGuid():N}";

            foreach (var other in FindObjectsByType<TimelineTimeProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != this && string.Equals(other._providerId, _providerId, StringComparison.Ordinal))
                {
                    _providerId = $"timeline.{Guid.NewGuid():N}";
                    break;
                }
            }
        }
#endif
    }
}
