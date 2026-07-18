using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightPosePatch
    {
        [SerializeField] private FanlightPoseFields _fields;
        [SerializeField] private FanlightPoseState _value;

        internal FanlightPoseFields Fields => _fields;
        internal FanlightPoseState Value => _value;

        internal FanlightPosePatch(FanlightPoseFields fields, FanlightPoseState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
