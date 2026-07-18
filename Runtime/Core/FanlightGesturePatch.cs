using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightGesturePatch
    {
        [SerializeField] private FanlightGestureFields _fields;
        [SerializeField] private FanlightGestureState _value;

        internal FanlightGestureFields Fields => _fields;
        internal FanlightGestureState Value => _value;

        internal FanlightGesturePatch(FanlightGestureFields fields, FanlightGestureState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
