using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightPalettePatch
    {
        [SerializeField] private FanlightPaletteFields _fields;
        [SerializeField] private FanlightPaletteState _value;

        internal FanlightPaletteFields Fields => _fields;
        internal FanlightPaletteState Value => _value;

        internal FanlightPalettePatch(FanlightPaletteFields fields, FanlightPaletteState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
