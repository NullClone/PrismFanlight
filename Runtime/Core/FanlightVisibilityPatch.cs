using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVisibilityPatch
    {
        [SerializeField] private FanlightVisibilityFields _fields;
        [SerializeField] private FanlightVisibilityState _value;

        internal FanlightVisibilityFields Fields => _fields;
        internal FanlightVisibilityState Value => _value;

        internal FanlightVisibilityPatch(FanlightVisibilityFields fields, FanlightVisibilityState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
