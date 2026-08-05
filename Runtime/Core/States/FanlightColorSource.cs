using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightColorSource
    {
        // Fields

        [SerializeField]
        private FanlightColorMode _mode;

        [SerializeField, ColorUsage(false, false)]
        private Color _slot1;

        [SerializeField, ColorUsage(false, false)]
        private Color _slot2;

        [SerializeField, ColorUsage(false, false)]
        private Color _slot3;

        [SerializeField, ColorUsage(false, false)]
        private Color _slot4;

        [SerializeField, ColorUsage(false, false)]
        private Color _slot5;

        [SerializeField, ColorUsage(false, false)]
        private Color _slot6;

        [SerializeField, ColorUsage(false, false)]
        private Color _colorA;

        [SerializeField, ColorUsage(false, false)]
        private Color _colorB;

        [SerializeField]
        private Vector2 _origin;

        [SerializeField]
        private Vector2 _direction;

        [SerializeField]
        private float _width;

        [SerializeField]
        private float _offset;

        [SerializeField]
        private FanlightBlockPaletteEntry[] _blockPaletteEntries;


        // Properties

        internal FanlightColorMode Mode => _mode;

        internal Color Slot1 => _slot1;

        internal Color Slot2 => _slot2;

        internal Color Slot3 => _slot3;

        internal Color Slot4 => _slot4;

        internal Color Slot5 => _slot5;

        internal Color Slot6 => _slot6;

        internal Color ColorA => _colorA;

        internal Color ColorB => _colorB;

        internal Vector2 Origin => _origin;

        internal Vector2 Direction => _direction;

        internal float Width => _width;

        internal float Offset => _offset;

        internal int BlockPaletteEntryCount => _blockPaletteEntries?.Length ?? 0;


        // Methods

        internal FanlightColorSource(
            FanlightColorMode mode,
            Color slot1,
            Color slot2,
            Color slot3,
            Color slot4,
            Color slot5,
            Color slot6,
            Color colorA,
            Color colorB,
            Vector2 origin,
            Vector2 direction,
            float width,
            float offset,
            FanlightBlockPaletteEntry[] blockPaletteEntries)
        {
            _mode = mode;
            _slot1 = slot1;
            _slot2 = slot2;
            _slot3 = slot3;
            _slot4 = slot4;
            _slot5 = slot5;
            _slot6 = slot6;
            _colorA = colorA;
            _colorB = colorB;
            _origin = origin;
            _direction = direction;
            _width = width;
            _offset = offset;
            _blockPaletteEntries = blockPaletteEntries == null
                ? Array.Empty<FanlightBlockPaletteEntry>()
                : (FanlightBlockPaletteEntry[])blockPaletteEntries.Clone();

            ValidateAndNormalize();
        }

        internal FanlightBlockPaletteEntry GetBlockPaletteEntry(int index) => _blockPaletteEntries[index];

        internal Color GetPaletteSlot(int index) => index switch
        {
            0 => _slot1,
            1 => _slot2,
            2 => _slot3,
            3 => _slot4,
            4 => _slot5,
            5 => _slot6,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal FanlightColorSource Validated()
        {
            return new FanlightColorSource(
                _mode,
                _slot1,
                _slot2,
                _slot3,
                _slot4,
                _slot5,
                _slot6,
                _colorA,
                _colorB,
                _origin,
                _direction,
                _width,
                _offset,
                _blockPaletteEntries);
        }

        internal bool ContentEquals(in FanlightColorSource other)
        {
            if (_mode != other._mode) return false;

            switch (_mode)
            {
                case FanlightColorMode.StablePalette:
                    return PaletteEquals(other);
                case FanlightColorMode.LinearGradient:
                    return _colorA.Equals(other._colorA)
                           && _colorB.Equals(other._colorB)
                           && _origin.Equals(other._origin)
                           && _direction.Equals(other._direction)
                           && _width.Equals(other._width)
                           && _offset.Equals(other._offset);
                case FanlightColorMode.BlockPalette:
                    if (!PaletteEquals(other) || BlockPaletteEntryCount != other.BlockPaletteEntryCount) return false;
                    for (var i = 0; i < BlockPaletteEntryCount; i++)
                    {
                        if (!GetBlockPaletteEntry(i).Equals(other.GetBlockPaletteEntry(i))) return false;
                    }

                    return true;
                default:
                    return false;
            }
        }

        private void ValidateAndNormalize()
        {
            switch (_mode)
            {
                case FanlightColorMode.StablePalette:
                    ValidatePalette();
                    break;
                case FanlightColorMode.LinearGradient:
                    ValidateChroma(_colorA, nameof(_colorA));
                    ValidateChroma(_colorB, nameof(_colorB));
                    _origin = FanlightStateValidation.RequireFinite(_origin, nameof(_origin));
                    _direction = FanlightStateValidation.RequireDirection(_direction, nameof(_direction));
                    _width = FanlightStateValidation.RequireMinimumExclusive(_width, 0f, nameof(_width));
                    _offset = FanlightStateValidation.RequireFinite(_offset, nameof(_offset));
                    break;
                case FanlightColorMode.BlockPalette:
                    ValidatePalette();
                    ValidateBlockPaletteEntries();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode));
            }
        }

        private void ValidatePalette()
        {
            ValidateChroma(_slot1, nameof(_slot1));
            ValidateChroma(_slot2, nameof(_slot2));
            ValidateChroma(_slot3, nameof(_slot3));
            ValidateChroma(_slot4, nameof(_slot4));
            ValidateChroma(_slot5, nameof(_slot5));
            ValidateChroma(_slot6, nameof(_slot6));
        }

        private void ValidateBlockPaletteEntries()
        {
            if (_blockPaletteEntries == null || _blockPaletteEntries.Length == 0)
            {
                throw new ArgumentException("Block Palette requires a complete Stable Block ID mapping.", nameof(_blockPaletteEntries));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _blockPaletteEntries.Length; i++)
            {
                var entry = _blockPaletteEntries[i];
                _ = new FanlightBlockPaletteEntry(entry.StableBlockId, entry.PaletteSlot);
                if (!ids.Add(entry.StableBlockId))
                {
                    throw new ArgumentException("Block Palette Stable Block IDs must be unique.", nameof(_blockPaletteEntries));
                }
            }
        }

        private bool PaletteEquals(in FanlightColorSource other)
        {
            return _slot1.Equals(other._slot1)
                   && _slot2.Equals(other._slot2)
                   && _slot3.Equals(other._slot3)
                   && _slot4.Equals(other._slot4)
                   && _slot5.Equals(other._slot5)
                   && _slot6.Equals(other._slot6);
        }

        private static void ValidateChroma(Color value, string name)
        {
            if (!FanlightStateValidation.IsFinite(value.r)
                || !FanlightStateValidation.IsFinite(value.g)
                || !FanlightStateValidation.IsFinite(value.b)
                || !FanlightStateValidation.IsFinite(value.a)
                || value.r < 0f
                || value.r > 1f
                || value.g < 0f
                || value.g > 1f
                || value.b < 0f
                || value.b > 1f
                || Mathf.Abs(value.a - 1f) > 0.0001f)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            Color.RGBToHSV(value, out _, out _, out var colorValue);
            if (Mathf.Abs(colorValue - 1f) > 0.0001f)
            {
                throw new ArgumentOutOfRangeException(name, "Chroma must use HSV Value 1.");
            }
        }
    }
}
