using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightGesturePatch
    {
        // Fields

        [SerializeField]
        private FanlightGestureFields _fields;

        [SerializeField]
        private FanlightGestureState _value;


        // Properties

        internal FanlightGestureFields Fields => _fields;

        internal FanlightGestureState Value => _value;


        // Methods

        internal FanlightGesturePatch(FanlightGestureFields fields, FanlightGestureState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
