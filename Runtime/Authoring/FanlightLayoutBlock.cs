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

        [SerializeField]
        private int _authoringRevision;


        // Properties

        public string BlockId => _blockId ?? string.Empty;

        public FanlightBlockPlacement Placement => _placement;

        public int AuthoringRevision => _authoringRevision;


        // Methods

        internal FanlightLayoutBlock(string blockId)
        {
            _blockId = blockId;
            _placement = FanlightBlockPlacement.Identity;
            _authoringRevision = 1;
        }

        internal void SetPlacement(FanlightBlockPlacement placement)
        {
            if (_authoringRevision == int.MaxValue) throw new InvalidOperationException("Block authoring revision is exhausted.");
            _placement = placement;
            _authoringRevision++;
        }
    }
}
