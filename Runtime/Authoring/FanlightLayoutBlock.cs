using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal sealed class FanlightLayoutBlock
    {
        // Fields

        [SerializeField]
        private string _blockId;

        [SerializeField]
        private FanlightBlockPlacement _placement;

        [SerializeField]
        private FanlightLayoutRow[] _rows;


        // Properties

        internal string BlockId => _blockId ?? string.Empty;

        internal FanlightBlockPlacement Placement => _placement;

        internal int RowCount => _rows?.Length ?? 0;

        internal int SeatCount
        {
            get
            {
                if (_rows == null) return 0;

                long count = 0;
                for (var i = 0; i < _rows.Length; i++)
                {
                    if (_rows[i] != null) count += _rows[i].SeatCount;
                    if (count > int.MaxValue) return 0;
                }

                return (int)count;
            }
        }


        // Methods

        internal FanlightLayoutBlock(
            string blockId,
            FanlightBlockPlacement placement,
            FanlightLayoutRow[] rows)
        {
            _blockId = blockId;
            _placement = placement;
            _rows = rows == null ? Array.Empty<FanlightLayoutRow>() : (FanlightLayoutRow[])rows.Clone();
        }

        internal FanlightLayoutRow GetRow(int rowIndex) => _rows[rowIndex];

        internal FanlightLayoutRow[] CopyRows() => _rows == null ? Array.Empty<FanlightLayoutRow>() : (FanlightLayoutRow[])_rows.Clone();

        internal void SetPlacement(FanlightBlockPlacement placement)
        {
            _placement = placement;
        }

        internal void SetRows(FanlightLayoutRow[] rows)
        {
            _rows = rows == null ? Array.Empty<FanlightLayoutRow>() : (FanlightLayoutRow[])rows.Clone();
        }
    }
}
