using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightNoisePatch
    {
        // Fields

        [SerializeField]
        private FanlightNoiseFields _fields;

        [SerializeField]
        private FanlightNoiseState _value;


        // Properties

        internal FanlightNoiseFields Fields => _fields;

        internal FanlightNoiseState Value => _value;


        // Methods

        internal FanlightNoisePatch(FanlightNoiseFields fields, FanlightNoiseState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
