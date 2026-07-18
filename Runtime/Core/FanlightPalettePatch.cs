using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightPalettePatch
    {
        // Fields

        [SerializeField]
        private FanlightPaletteFields _fields;

        [SerializeField]
        private FanlightPaletteState _value;


        // Properties

        internal FanlightPaletteFields Fields => _fields;

        internal FanlightPaletteState Value => _value;


        // Methods

        internal FanlightPalettePatch(FanlightPaletteFields fields, FanlightPaletteState value)
        {
            _fields = fields;
            _value = value;
        }
    }
}
