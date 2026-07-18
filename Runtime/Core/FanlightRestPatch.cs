using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightRestPatch
    {
        [SerializeField] private FanlightRestFields _fields;
        [SerializeField] private FanlightRestState _value;

        internal FanlightRestFields Fields => _fields;
        internal FanlightRestState Value => _value;

        internal FanlightRestPatch(FanlightRestFields fields, FanlightRestState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
