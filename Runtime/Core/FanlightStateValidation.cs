using System;

namespace PrismFanlight.Core
{
    internal static class FanlightStateValidation
    {
        internal static float RequireRange(float value, float minimum, float maximum, string name)
        {
            if (!IsFinite(value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }

        internal static float RequireMinimum(float value, float minimum, string name)
        {
            if (!IsFinite(value) || value < minimum)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }

        internal static float RequireFinite(float value, string name)
        {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(name);
            return value;
        }

        internal static double RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
            return value;
        }

        internal static float NormalizeDegrees(float value, string name)
        {
            RequireFinite(value, name);
            value %= 360f;
            return value < 0f ? value + 360f : value;
        }

        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
