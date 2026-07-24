using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntensityPatch
    {
        // Fields

        [SerializeField]
        private FanlightIntensityFields _fields;

        [SerializeField]
        private FanlightIntensityState _value;


        // Properties

        internal FanlightIntensityFields Fields => _fields;

        internal FanlightIntensityState Value => _value;


        // Methods

        internal FanlightIntensityPatch(FanlightIntensityFields fields, FanlightIntensityState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
