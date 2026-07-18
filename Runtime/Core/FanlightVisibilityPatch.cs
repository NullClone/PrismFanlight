using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVisibilityPatch
    {
        // Fields

        [SerializeField]
        private FanlightVisibilityFields _fields;

        [SerializeField]
        private FanlightVisibilityState _value;


        // Properties

        internal FanlightVisibilityFields Fields => _fields;

        internal FanlightVisibilityState Value => _value;


        // Methods

        internal FanlightVisibilityPatch(FanlightVisibilityFields fields, FanlightVisibilityState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
