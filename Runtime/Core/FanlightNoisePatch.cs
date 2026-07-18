using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightNoisePatch
    {
        [SerializeField] private FanlightNoiseFields _fields;
        [SerializeField] private FanlightNoiseState _value;

        internal FanlightNoiseFields Fields => _fields;
        internal FanlightNoiseState Value => _value;

        internal FanlightNoisePatch(FanlightNoiseFields fields, FanlightNoiseState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
