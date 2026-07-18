using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightRestPatch
    {
        // Fields

        [SerializeField]
        private FanlightRestFields _fields;

        [SerializeField]
        private FanlightRestState _value;


        // Properties

        internal FanlightRestFields Fields => _fields;

        internal FanlightRestState Value => _value;


        // Methods

        internal FanlightRestPatch(FanlightRestFields fields, FanlightRestState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
