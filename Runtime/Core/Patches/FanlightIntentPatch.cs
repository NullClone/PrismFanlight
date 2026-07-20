using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntentPatch
    {
        // Fields

        [SerializeField]
        private FanlightIntentFields _fields;

        [SerializeField]
        private FanlightIntentState _value;


        // Properties

        internal FanlightIntentFields Fields => _fields;

        internal FanlightIntentState Value => _value;


        // Methods

        internal FanlightIntentPatch(FanlightIntentFields fields, FanlightIntentState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
