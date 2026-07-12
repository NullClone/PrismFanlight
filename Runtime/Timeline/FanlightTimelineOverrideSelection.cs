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


        public IReadOnlyList<string> Paths
        {
            get
            {
                EnsurePaths();
                return _paths;
            }
        }


        public bool Contains(string path)
        {
            EnsurePaths();

            return _paths.Contains(path);
        }

        public void Set(string path, bool enabled)
        {
            EnsurePaths();

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

        private void EnsurePaths()
        {
            _paths ??= new List<string>();
        }
    }
}
