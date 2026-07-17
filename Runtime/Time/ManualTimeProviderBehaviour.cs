using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    [AddComponentMenu("Prism Fanlight/Time Providers/Manual Time Provider")]
    public sealed class ManualTimeProviderBehaviour : MonoBehaviour, IShowTimeProvider
    {
        [SerializeField] private string _providerId = string.Empty;
        [SerializeField] private double _seconds;
        [SerializeField] private double _rate;
        [SerializeField] private FanlightClockStatus _status = FanlightClockStatus.Holding;
        [SerializeField] private FanlightTimeDiscontinuity _nextDiscontinuity;
        private long _sequence;

        public string ProviderId => _providerId ?? string.Empty;
        public void SetTime(double seconds, double rate, FanlightTimeDiscontinuity discontinuity = FanlightTimeDiscontinuity.Seek)
        {
            _seconds = seconds;
            _rate = rate;
            _status = rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready;
            _nextDiscontinuity = discontinuity;
        }

        public void SetStatus(FanlightClockStatus status) => _status = status;

        public ShowTimeProviderSample Sample()
        {
            var discontinuity = _nextDiscontinuity;
            _nextDiscontinuity = FanlightTimeDiscontinuity.None;
            return new ShowTimeProviderSample(ProviderId, _seconds, _rate, _status, discontinuity, ++_sequence);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_providerId)) _providerId = $"manual.{Guid.NewGuid():N}";
            foreach (var other in FindObjectsByType<ManualTimeProviderBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != this && string.Equals(other._providerId, _providerId, StringComparison.Ordinal))
                {
                    _providerId = $"manual.{Guid.NewGuid():N}";
                    break;
                }
            }
        }
#endif
    }
}
