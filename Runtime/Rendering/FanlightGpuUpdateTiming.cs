using System;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightGpuUpdateTiming
    {
        // Fields

        [SerializeField]
        private FanlightGpuUpdateMode _mode;

        [SerializeField, Min(1.0f)]
        private float _targetFrameRate;


        // Properties

        public FanlightGpuUpdateMode Mode => _mode;

        public float TargetFrameRate => Mathf.Max(1.0f, _targetFrameRate);


        // Methods

        public static FanlightGpuUpdateTiming EveryFrame()
        {
            return new FanlightGpuUpdateTiming
            {
                _mode = FanlightGpuUpdateMode.EveryFrame,
                _targetFrameRate = 60.0f
            };
        }

        public static FanlightGpuUpdateTiming FixedRate(float targetFrameRate)
        {
            return new FanlightGpuUpdateTiming
            {
                _mode = FanlightGpuUpdateMode.FixedRate,
                _targetFrameRate = Mathf.Max(1.0f, targetFrameRate)
            };
        }

        public FanlightGpuUpdateTiming Validated()
        {
            return new FanlightGpuUpdateTiming
            {
                _mode = _mode,
                _targetFrameRate = TargetFrameRate
            };
        }

        public string ToDisplayString()
        {
            return _mode == FanlightGpuUpdateMode.EveryFrame
                ? "Every Frame"
                : $"{TargetFrameRate:0.#} FPS";
        }
    }
}
