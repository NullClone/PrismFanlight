using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    public struct FanlightLayoutBlock
    {
        // Fields

        [SerializeField]
        private string _blockId;

        [SerializeField]
        private FanlightBlockPlacement _placement;

        // Properties

        public string BlockId => _blockId ?? string.Empty;

        public FanlightBlockPlacement Placement => _placement;

        // Methods

        internal FanlightLayoutBlock(string blockId)
        {
            _blockId = blockId;
            _placement = FanlightBlockPlacement.Identity;
        }

        internal void SetPlacement(FanlightBlockPlacement placement)
        {
            _placement = placement;
        }
    }
}
