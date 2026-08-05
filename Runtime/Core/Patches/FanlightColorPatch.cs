using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightColorPatch
    {
        // Fields

        [SerializeField]
        private FanlightColorFields _fields;

        [SerializeField]
        private FanlightColorState _value;


        // Properties

        internal FanlightColorFields Fields => _fields;

        internal FanlightColorState Value => _value;


        // Methods

        internal FanlightColorPatch(FanlightColorFields fields, FanlightColorState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
