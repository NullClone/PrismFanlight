using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightPaletteState
    {
        [SerializeField] private Color _slot1;
        [SerializeField] private Color _slot2;
        [SerializeField] private Color _slot3;
        [SerializeField] private Color _slot4;
        [SerializeField] private Color _slot5;
        [SerializeField] private Color _slot6;
        [SerializeField] private float _globalIntensity;
        [SerializeField] private float _randomIntensity;

        internal Color Slot1 => _slot1;
        internal Color Slot2 => _slot2;
        internal Color Slot3 => _slot3;
        internal Color Slot4 => _slot4;
        internal Color Slot5 => _slot5;
        internal Color Slot6 => _slot6;
        internal float GlobalIntensity => _globalIntensity;
        internal float RandomIntensity => _randomIntensity;

        internal FanlightPaletteState(
            Color slot1,
            Color slot2,
            Color slot3,
            Color slot4,
            Color slot5,
            Color slot6,
            float globalIntensity,
            float randomIntensity)
        {
            ValidateColor(slot1, nameof(slot1));
            ValidateColor(slot2, nameof(slot2));
            ValidateColor(slot3, nameof(slot3));
            ValidateColor(slot4, nameof(slot4));
            ValidateColor(slot5, nameof(slot5));
            ValidateColor(slot6, nameof(slot6));
            _slot1 = slot1;
            _slot2 = slot2;
            _slot3 = slot3;
            _slot4 = slot4;
            _slot5 = slot5;
            _slot6 = slot6;
            _globalIntensity = FanlightStateValidation.RequireMinimum(globalIntensity, 0f, nameof(globalIntensity));
            _randomIntensity = FanlightStateValidation.RequireRange(randomIntensity, 0f, 1f, nameof(randomIntensity));
        }

        private static void ValidateColor(Color value, string name)
        {
            if (!FanlightStateValidation.IsFinite(value.r)
                || !FanlightStateValidation.IsFinite(value.g)
                || !FanlightStateValidation.IsFinite(value.b)
                || !FanlightStateValidation.IsFinite(value.a))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
