using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightDirectionPatch
    {
        [SerializeField] private FanlightDirectionFields _fields;
        [SerializeField] private FanlightDirectionState _value;

        internal FanlightDirectionFields Fields => _fields;
        internal FanlightDirectionState Value => _value;

        internal FanlightDirectionPatch(FanlightDirectionFields fields, FanlightDirectionState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
