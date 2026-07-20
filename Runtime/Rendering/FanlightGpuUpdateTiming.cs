using System;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    internal struct FanlightGpuUpdateTiming
    {
        // Fields

        [SerializeField]
        private FanlightGpuUpdateMode _mode;

        [SerializeField, Min(1.0f)]
        private float _targetFrameRate;


        // Properties

        internal FanlightGpuUpdateMode Mode => _mode;

        internal float TargetFrameRate => Mathf.Max(1.0f, _targetFrameRate);


        // Methods

        internal static FanlightGpuUpdateTiming EveryFrame()
        {
            return new FanlightGpuUpdateTiming
            {
                _mode = FanlightGpuUpdateMode.EveryFrame,
                _targetFrameRate = 60.0f
            };
        }

        internal static FanlightGpuUpdateTiming FixedRate(float targetFrameRate)
        {
            return new FanlightGpuUpdateTiming
            {
                _mode = FanlightGpuUpdateMode.FixedRate,
                _targetFrameRate = Mathf.Max(1.0f, targetFrameRate)
            };
        }

        internal FanlightGpuUpdateTiming Validated()
        {
            return new FanlightGpuUpdateTiming
            {
                _mode = _mode,
                _targetFrameRate = TargetFrameRate
            };
        }

        internal string ToDisplayString()
        {
            return _mode == FanlightGpuUpdateMode.EveryFrame
                ? "Every Frame"
                : $"{TargetFrameRate:0.#} FPS";
        }
    }
}
