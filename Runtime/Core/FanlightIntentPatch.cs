using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntentPatch
    {
        [SerializeField] private FanlightIntentFields _fields;
        [SerializeField] private FanlightIntentState _value;

        internal FanlightIntentFields Fields => _fields;
        internal FanlightIntentState Value => _value;

        internal FanlightIntentPatch(FanlightIntentFields fields, FanlightIntentState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
