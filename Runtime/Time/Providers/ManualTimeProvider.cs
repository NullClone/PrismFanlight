using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Prism Fanlight/Time Providers/Manual Time Provider")]
    public sealed class ManualTimeProvider : MonoBehaviour, IShowTimeProvider
    {
        // Fields

        [SerializeField]
        private double _seconds;

        [SerializeField]
        private double _rate;

        [SerializeField]
        private FanlightTimeDiscontinuity _nextDiscontinuity;


        // Methods

        internal void SetTime(double seconds, double rate, FanlightTimeDiscontinuity discontinuity = FanlightTimeDiscontinuity.Seek)
        {
            _seconds = seconds;
            _rate = rate;
            _nextDiscontinuity = discontinuity;
        }


        ShowTimeProviderSample IShowTimeProvider.Sample()
        {
            var discontinuity = _nextDiscontinuity;
            _nextDiscontinuity = FanlightTimeDiscontinuity.None;
            var status = _rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready;

            return new ShowTimeProviderSample(_seconds, _rate, status, discontinuity);
        }
    }
}
