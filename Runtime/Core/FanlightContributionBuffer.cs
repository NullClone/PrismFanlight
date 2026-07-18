using System;

namespace PrismFanlight.Core
{
    internal sealed class FanlightContributionBuffer
    {
        private FanlightShowContribution[] _items;

        internal int Count { get; private set; }

        internal FanlightContributionBuffer(int capacity)
        {
            _items = new FanlightShowContribution[Math.Max(1, capacity)];
        }

        internal void Add(FanlightShowContribution contribution)
        {
            if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
            _items[Count++] = contribution;
        }

        internal void Clear() => Count = 0;

        internal FanlightShowContribution GetAt(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }

        internal ReadOnlyMemory<FanlightShowContribution> AsMemory() => new(_items, 0, Count);
    }
}
