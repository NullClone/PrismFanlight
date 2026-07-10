using UnityEngine;

namespace PrismFanlight
{
    internal static class FanlightStateComposer
    {
        public static FanlightResolvedState ApplyColor(
            FanlightResolvedState baseState,
            Color weightedColor,
            float weightedIntensity,
            float totalWeight)
        {
            if (totalWeight <= 0.0f) return baseState;

            var blendWeight = Mathf.Clamp01(totalWeight);
            var inverseTotalWeight = 1.0f / totalWeight;
            var color = baseState.Color;
            color.mode = FanlightColorMode.Single;
            color.primaryColor = Color.Lerp(
                color.primaryColor,
                weightedColor * inverseTotalWeight,
                blendWeight);
            color.intensity = Mathf.Lerp(
                color.intensity,
                weightedIntensity * inverseTotalWeight,
                blendWeight);

            return new FanlightResolvedState(
                baseState.Tempo,
                baseState.Motion,
                color,
                baseState.Audience,
                baseState.Lod,
                baseState.Random,
                baseState.SwingTargetWorldPosition,
                baseState.LocalToWorld,
                baseState.Time,
                baseState.UpdateClock,
                baseState.IsTimeJump);
        }
    }
}
