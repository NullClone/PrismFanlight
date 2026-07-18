using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightAudienceBodyPatch
    {
        [SerializeField] private FanlightAudienceBodyFields _fields;
        [SerializeField] private FanlightAudienceBodyState _value;

        internal FanlightAudienceBodyFields Fields => _fields;
        internal FanlightAudienceBodyState Value => _value;

        internal FanlightAudienceBodyPatch(FanlightAudienceBodyFields fields, FanlightAudienceBodyState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
