using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    internal static class FanlightStateValidation
    {
        internal static float RequireRange(float value, float minimum, float maximum, string name)
        {
            if (!IsFinite(value) || value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static float RequireMinimum(float value, float minimum, string name)
        {
            if (!IsFinite(value) || value < minimum)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static float RequireMinimumExclusive(float value, float minimum, string name)
        {
            if (!IsFinite(value) || value <= minimum)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static float RequireFinite(float value, string name)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static double RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static Vector3 RequireFinite(Vector3 value, string name)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static Vector2 RequireFinite(Vector2 value, string name)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        internal static Vector3 RequireDirection(Vector3 value, string name)
        {
            RequireFinite(value, name);

            if (value.sqrMagnitude <= 0.000001f)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            var scale = Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
            var scaled = value / scale;
            return scaled / scaled.magnitude;
        }

        internal static Vector2 RequireDirection(Vector2 value, string name)
        {
            RequireFinite(value, name);

            if (value.sqrMagnitude <= 0.000001f)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            var scale = Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y));
            var scaled = value / scale;
            return scaled / scaled.magnitude;
        }

        internal static float NormalizeDegrees(float value, string name)
        {
            RequireFinite(value, name);

            value %= 360f;
            return value < 0f ? value + 360f : value;
        }

        internal static float LerpShortestDegrees(float current, float incoming, float weight)
        {
            current = NormalizeDegrees(current, nameof(current));
            incoming = NormalizeDegrees(incoming, nameof(incoming));
            weight = RequireRange(weight, 0f, 1f, nameof(weight));

            var delta = Mathf.Repeat(incoming - current + 180f, 360f) - 180f;
            return Mathf.Repeat(current + delta * weight, 360f);
        }

        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        internal static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
    }
}
