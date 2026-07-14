using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPaletteGradientPlayableBehaviour : PlayableBehaviour
    {
        public FanlightPaletteGradientPlayableAsset Asset;

        internal FanlightColorSettings Evaluate(float normalizedTime)
        {
            var result = FanlightColorSettings.Default();
            if (Asset == null) return result;

            for (var i = 0; i < FanlightColorSettings.PaletteSlotCount; i++)
            {
                if (Asset.OverridesSlot(i)) result = result.WithSlot(i, Asset.EvaluateSlot(i, normalizedTime));
            }

            return result;
        }
    }
}
