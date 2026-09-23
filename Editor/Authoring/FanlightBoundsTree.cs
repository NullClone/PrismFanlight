using System.Collections.Generic;
using PrismFanlight.Rendering;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightBoundsTree
    {
        // Fields

        private readonly int _count;
        private readonly int _size;
        private readonly Bounds[] _nodes;
        private readonly bool[] _valid;


        // Properties

        internal Bounds Root => _valid[1] ? _nodes[1] : new Bounds(Vector3.zero, Vector3.one);


        // Methods

        internal FanlightBoundsTree(int count)
        {
            _count = count;
            _size = 1;
            while (_size < count) _size <<= 1;
            _nodes = new Bounds[_size * 2];
            _valid = new bool[_size * 2];
        }

        internal void Update(int index, Bounds bounds)
        {
            var node = _size + index;
            _nodes[node] = bounds;
            _valid[node] = true;
            while ((node >>= 1) > 0) Rebuild(node);
        }

        internal void Query(Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            results.Clear();

            QueryNode(1, planes, localToWorld, results);
        }


        private void QueryNode(int node, Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            if (node >= _nodes.Length || !_valid[node]) return;

            var worldBounds = FanlightGeometryBuilder.TransformBounds(localToWorld, _nodes[node]);

            if (!GeometryUtility.TestPlanesAABB(planes, worldBounds)) return;

            if (node >= _size)
            {
                var index = node - _size;
                if (index < _count) results.Add(index);
                return;
            }

            QueryNode(node * 2, planes, localToWorld, results);
            QueryNode(node * 2 + 1, planes, localToWorld, results);
        }

        private void Rebuild(int node)
        {
            var left = node * 2;
            var right = left + 1;

            if (!_valid[left] && !_valid[right])
            {
                _valid[node] = false;
                return;
            }

            if (!_valid[right])
            {
                _nodes[node] = _nodes[left];
                _valid[node] = true;
                return;
            }

            if (!_valid[left])
            {
                _nodes[node] = _nodes[right];
                _valid[node] = true;
                return;
            }

            var bounds = _nodes[left];
            bounds.Encapsulate(_nodes[right].min);
            bounds.Encapsulate(_nodes[right].max);

            _nodes[node] = bounds;
            _valid[node] = true;
        }
    }
}
