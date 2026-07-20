using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightAudienceBodyPatch
    {
        // Fields

        [SerializeField]
        private FanlightAudienceBodyFields _fields;

        [SerializeField]
        private FanlightAudienceBodyState _value;


        // Properties

        internal FanlightAudienceBodyFields Fields => _fields;

        internal FanlightAudienceBodyState Value => _value;


        // Methods

        internal FanlightAudienceBodyPatch(FanlightAudienceBodyFields fields, FanlightAudienceBodyState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
