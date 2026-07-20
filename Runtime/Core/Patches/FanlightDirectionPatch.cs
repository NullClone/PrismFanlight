using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightDirectionPatch
    {
        // Fields

        [SerializeField]
        private FanlightDirectionFields _fields;

        [SerializeField]
        private FanlightDirectionState _value;


        // Properties

        internal FanlightDirectionFields Fields => _fields;

        internal FanlightDirectionState Value => _value;


        // Methods

        internal FanlightDirectionPatch(FanlightDirectionFields fields, FanlightDirectionState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
