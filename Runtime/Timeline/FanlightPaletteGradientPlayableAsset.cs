using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightPaletteGradientPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField]
        private FanlightPaletteSlotMask _slots = FanlightPaletteSlotMask.All;

        [SerializeField, GradientUsage(true)]
        private Gradient _slot1 = null;

        [SerializeField, GradientUsage(true)]
        private Gradient _slot2 = null;

        [SerializeField, GradientUsage(true)]
        private Gradient _slot3 = null;

        [SerializeField, GradientUsage(true)]
        private Gradient _slot4 = null;

        [SerializeField, GradientUsage(true)]
        private Gradient _slot5 = null;

        [SerializeField, GradientUsage(true)]
        private Gradient _slot6 = null;

        private static readonly string[] SlotPaths =
        {
            "color.slot1",
            "color.slot2",
            "color.slot3",
            "color.slot4",
            "color.slot5",
            "color.slot6"
        };

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public FanlightPaletteSlotMask Slots => _slots;


        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            EnsureGradients();
            var playable = ScriptPlayable<FanlightPaletteGradientPlayableBehaviour>.Create(graph);
            playable.GetBehaviour().Asset = this;
            return playable;
        }

        private void OnValidate()
        {
            EnsureGradients();
        }

        internal Color EvaluateSlot(int index, float normalizedTime)
        {
            EnsureGradients();
            return GetGradient(index).Evaluate(Mathf.Clamp01(normalizedTime));
        }

        internal bool OverridesSlot(int index)
        {
            if (index < 0 || index >= FanlightColorSettings.PaletteSlotCount) return false;
            return (_slots & (FanlightPaletteSlotMask)(1 << index)) != 0;
        }

        internal IEnumerable<string> GetOverridePaths()
        {
            for (var i = 0; i < SlotPaths.Length; i++)
            {
                if (OverridesSlot(i)) yield return SlotPaths[i];
            }
        }

        internal int GetStableHash()
        {
            EnsureGradients();

            unchecked
            {
                var hash = 17;
                hash = hash * 31 + _slots.GetHashCode();
                for (var i = 0; i < FanlightColorSettings.PaletteSlotCount; i++)
                {
                    if (!OverridesSlot(i)) continue;
                    var gradient = GetGradient(i);
                    hash = hash * 31 + gradient.mode.GetHashCode();
                    foreach (var key in gradient.colorKeys) hash = hash * 31 + key.GetHashCode();
                    foreach (var key in gradient.alphaKeys) hash = hash * 31 + key.GetHashCode();
                }

                return hash;
            }
        }

        private Gradient GetGradient(int index)
        {
            return index switch
            {
                0 => _slot1,
                1 => _slot2,
                2 => _slot3,
                3 => _slot4,
                4 => _slot5,
                5 => _slot6,
                _ => _slot1
            };
        }

        private void EnsureGradients()
        {
            _slot1 ??= CreateConstantGradient(Color.white);
            _slot2 ??= CreateConstantGradient(Color.white);
            _slot3 ??= CreateConstantGradient(Color.white);
            _slot4 ??= CreateConstantGradient(Color.white);
            _slot5 ??= CreateConstantGradient(Color.white);
            _slot6 ??= CreateConstantGradient(Color.white);
        }

        private static Gradient CreateConstantGradient(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f) });
            return gradient;
        }
    }
}
