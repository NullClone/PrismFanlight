using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVariationPatch
    {
        // Fields

        [SerializeField]
        private FanlightVariationFields _fields;

        [SerializeField]
        private FanlightVariationState _value;


        // Properties

        internal FanlightVariationFields Fields => _fields;

        internal FanlightVariationState Value => _value;


        // Methods

        internal FanlightVariationPatch(FanlightVariationFields fields, FanlightVariationState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
