using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightMotionClip : FanlightTimelineClipAsset
    {
        // Fields

        [SerializeField]
        private FanlightMotionState _value = FanlightTimelineDefaults.MotionState();


        // Properties

        internal override FanlightTimelineClipValue Value
        {
            get
            {
#if UNITY_EDITOR
                var value = FanlightShowStateAuthoringValidator.Validate(_value);
#else
                var value = _value;
#endif
                if (value.MotionAsset == null || !value.MotionAsset.HasValidBake)
                {
                    throw new InvalidOperationException("Motion clips require a baked Motion Asset.");
                }

                return FanlightTimelineClipValue.From(value);
            }
        }


        // Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            _value = FanlightShowStateAuthoringValidator.Validate(_value);
        }
#endif
    }
}
