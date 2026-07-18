using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVisibilityState
    {
        [SerializeField] private bool _penlightsEnabled;
        [SerializeField] private bool _audienceBodiesEnabled;

        internal bool PenlightsEnabled => _penlightsEnabled;
        internal bool AudienceBodiesEnabled => _audienceBodiesEnabled;

        internal FanlightVisibilityState(bool penlightsEnabled, bool audienceBodiesEnabled)
        {
            _penlightsEnabled = penlightsEnabled;
            _audienceBodiesEnabled = audienceBodiesEnabled;
        }
    }
}
