using System;
using UnityEngine;

namespace PrismFanlight
{
    public enum FanlightGpuUpdateMode
    {
        EveryFrame,
        FixedRate
    }

    [Serializable]
    public struct FanlightGpuUpdateTiming
    {
        [SerializeField]
        private FanlightGpuUpdateMode _mode;

        [SerializeField, Min(1.0f)]
        private float _targetFrameRate;

        public FanlightGpuUpdateMode Mode => _mode;

        public float TargetFrameRate => Mathf.Max(1.0f, _targetFrameRate);

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
