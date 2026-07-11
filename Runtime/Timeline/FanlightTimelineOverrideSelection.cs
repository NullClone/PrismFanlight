using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    [Serializable]
    public sealed class FanlightTimelineOverrideSelection
    {
        [SerializeField]
        private List<string> _paths = new();

        public IReadOnlyList<string> Paths => _paths;

        public bool Contains(string path) => _paths.Contains(path);

        public void Set(string path, bool enabled)
        {
            if (enabled)
            {
                if (!_paths.Contains(path)) _paths.Add(path);
                return;
            }

            _paths.Remove(path);
        }

        public void SetAll(IEnumerable<string> paths, bool enabled)
        {
            foreach (var path in paths)
            {
                Set(path, enabled);
            }
        }
    }
}
