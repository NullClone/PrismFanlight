using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVariationPatch
    {
        [SerializeField] private FanlightVariationFields _fields;
        [SerializeField] private FanlightVariationState _value;

        internal FanlightVariationFields Fields => _fields;
        internal FanlightVariationState Value => _value;

        internal FanlightVariationPatch(FanlightVariationFields fields, FanlightVariationState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
