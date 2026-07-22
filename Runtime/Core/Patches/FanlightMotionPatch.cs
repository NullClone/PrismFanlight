using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightMotionPatch
    {
        // Fields

        [SerializeField]
        private FanlightMotionFields _fields;

        [SerializeField]
        private FanlightMotionState _value;


        // Properties

        internal FanlightMotionFields Fields => _fields;

        internal FanlightMotionState Value => _value;


        // Methods

        internal FanlightMotionPatch(FanlightMotionFields fields, FanlightMotionState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
