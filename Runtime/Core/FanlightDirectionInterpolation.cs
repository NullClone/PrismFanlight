using UnityEngine;

namespace PrismFanlight.Core
{
    internal static class FanlightDirectionInterpolation
    {
        // Fields

        private const float ParallelThreshold = 0.9995f;
        private const float AntiparallelThreshold = -0.999999f;


        // Methods

        internal static Vector3 Interpolate(Vector3 from, Vector3 to, float weight)
        {
            from = FanlightStateValidation.RequireDirection(from, nameof(from));
            to = FanlightStateValidation.RequireDirection(to, nameof(to));
            weight = Mathf.Clamp01(FanlightStateValidation.RequireFinite(weight, nameof(weight)));

            if (weight <= 0f) return from;
            if (weight >= 1f) return to;

            var cosine = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);

            if (cosine >= ParallelThreshold)
            {
                return FanlightStateValidation.RequireDirection(
                    Vector3.LerpUnclamped(from, to, weight),
                    nameof(weight));
            }

            if (cosine <= AntiparallelThreshold)
            {
                var axis = FallbackAxis(from);
                var angle = Mathf.PI * weight;
                var result = from * Mathf.Cos(angle) + Vector3.Cross(axis, from) * Mathf.Sin(angle);
                return FanlightStateValidation.RequireDirection(result, nameof(weight));
            }

            var theta = Mathf.Acos(cosine);
            var inverseSinTheta = 1f / Mathf.Sin(theta);
            var fromWeight = Mathf.Sin((1f - weight) * theta) * inverseSinTheta;
            var toWeight = Mathf.Sin(weight * theta) * inverseSinTheta;
            return FanlightStateValidation.RequireDirection(from * fromWeight + to * toWeight, nameof(weight));
        }

        private static Vector3 FallbackAxis(Vector3 direction)
        {
            var absolute = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            var reference = absolute.x <= absolute.y && absolute.x <= absolute.z
                ? Vector3.right
                : absolute.y <= absolute.z
                    ? Vector3.up
                    : Vector3.forward;
            return FanlightStateValidation.RequireDirection(Vector3.Cross(direction, reference), nameof(direction));
        }
    }
}
