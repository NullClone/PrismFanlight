using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    [AddComponentMenu("Prism Fanlight/Time Providers/Manual Time Provider")]
    public sealed class ManualTimeProviderBehaviour : MonoBehaviour, IShowTimeProvider
    {
        // Fields

        [SerializeField]
        private double _seconds;

        [SerializeField]
        private double _rate;

        [SerializeField]
        private FanlightClockStatus _status = FanlightClockStatus.Holding;

        [SerializeField]
        private FanlightTimeDiscontinuity _nextDiscontinuity;

        // Methods

        public void SetTime(double seconds, double rate, FanlightTimeDiscontinuity discontinuity = FanlightTimeDiscontinuity.Seek)
        {
            _seconds = seconds;
            _rate = rate;
            _status = rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready;
            _nextDiscontinuity = discontinuity;
        }

        public void SetStatus(FanlightClockStatus status) => _status = status;

        ShowTimeProviderSample IShowTimeProvider.Sample()
        {
            var discontinuity = _nextDiscontinuity;
            _nextDiscontinuity = FanlightTimeDiscontinuity.None;

            return new ShowTimeProviderSample(_seconds, _rate, _status, discontinuity);
        }
    }
}
