using System;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightRandomSettings
    {
        public uint globalSeed;

        public bool deterministic;

        public static FanlightRandomSettings Default() => new()
        {
            globalSeed = 1u,
            deterministic = true
        };

        public FanlightRandomSettings Validated()
        {
            var uninitialized = globalSeed == 0u && !deterministic;

            return new FanlightRandomSettings
            {
                globalSeed = uninitialized ? Default().globalSeed : globalSeed,
                deterministic = uninitialized || deterministic
            };
        }

        public int GetStableHash()
        {
            unchecked
            {
                return ((int)globalSeed * 397) ^ deterministic.GetHashCode();
            }
        }
    }
}
