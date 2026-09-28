using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVisibilityState
    {
        // Fields

        [SerializeField]
        private bool _penlightsEnabled;

        [SerializeField]
        private bool _audienceBodiesEnabled;


        // Properties

        internal bool PenlightsEnabled => _penlightsEnabled;

        internal bool AudienceBodiesEnabled => _audienceBodiesEnabled;


        // Methods

        internal FanlightVisibilityState(bool penlightsEnabled, bool audienceBodiesEnabled)
        {
            _penlightsEnabled = penlightsEnabled;
            _audienceBodiesEnabled = audienceBodiesEnabled;
        }
    }
}
