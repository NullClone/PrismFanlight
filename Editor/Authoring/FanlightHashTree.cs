namespace PrismFanlight.Editor
{
    internal sealed class FanlightHashTree
    {
        // Fields

        private readonly int _size;
        private readonly ulong[] _nodes;


        // Properties

        internal ulong Root => _nodes[1];


        // Methods

        internal FanlightHashTree(int count)
        {
            _size = 1;

            while (_size < count) _size <<= 1;

            _nodes = new ulong[_size * 2];
        }

        internal void Update(int index, ulong value)
        {
            var node = _size + index;

            _nodes[node] = value;

            while ((node >>= 1) > 0)
            {
                var hash = FanlightStableHash.Begin();
                hash = FanlightStableHash.Add(hash, _nodes[node * 2]);
                hash = FanlightStableHash.Add(hash, _nodes[node * 2 + 1]);
                _nodes[node] = FanlightStableHash.Finish(hash);
            }
        }
    }
}
