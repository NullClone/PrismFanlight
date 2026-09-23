using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEditor;

namespace PrismFanlight.Editor
{
    internal static class FanlightLayoutSelection
    {
        // Fields

        private static readonly Dictionary<string, List<string>> SelectedBlockIds = new(StringComparer.Ordinal);
        internal static event Action Changed;


        // Methods

        internal static int GetActiveIndex(FanlightLayoutAsset layout)
        {
            if (layout == null
                || !SelectedBlockIds.TryGetValue(layout.LayoutId.Value, out var blockIds)
                || blockIds.Count == 0)
            {
                return -1;
            }

            return FindBlockIndex(layout, blockIds[^1]);
        }

        internal static void GetIndices(FanlightLayoutAsset layout, List<int> results)
        {
            results.Clear();

            if (layout == null || !SelectedBlockIds.TryGetValue(layout.LayoutId.Value, out var blockIds)) return;

            for (var i = 0; i < blockIds.Count; i++)
            {
                var index = FindBlockIndex(layout, blockIds[i]);
                if (index >= 0) results.Add(index);
            }
        }

        internal static void SetOnly(FanlightLayoutAsset layout, int blockIndex)
        {
            if (layout == null || blockIndex < 0 || blockIndex >= layout.BlockCount) return;

            SelectedBlockIds[layout.LayoutId.Value] = new List<string>
            {
                layout.GetBlock(blockIndex).BlockId
            };
            NotifyChanged();
        }

        internal static void SetIndices(FanlightLayoutAsset layout, IReadOnlyList<int> blockIndices)
        {
            if (layout == null || blockIndices == null) return;

            var selected = new List<string>(blockIndices.Count);
            for (var i = 0; i < blockIndices.Count; i++)
            {
                var index = blockIndices[i];
                if (index < 0 || index >= layout.BlockCount) continue;

                var blockId = layout.GetBlock(index).BlockId;
                if (!selected.Contains(blockId)) selected.Add(blockId);
            }

            SelectedBlockIds[layout.LayoutId.Value] = selected;
            NotifyChanged();
        }

        internal static void Toggle(FanlightLayoutAsset layout, int blockIndex, bool additive)
        {
            if (layout == null || blockIndex < 0 || blockIndex >= layout.BlockCount) return;

            var key = layout.LayoutId.Value;
            if (!SelectedBlockIds.TryGetValue(key, out var selected))
            {
                selected = new List<string>();
                SelectedBlockIds[key] = selected;
            }

            var blockId = layout.GetBlock(blockIndex).BlockId;
            if (!additive)
            {
                selected.Clear();
                selected.Add(blockId);
            }
            else
            {
                var existing = selected.IndexOf(blockId);
                if (existing >= 0)
                {
                    selected.RemoveAt(existing);
                }
                else
                {
                    selected.Add(blockId);
                }
            }

            NotifyChanged();
        }

        internal static void SelectAll(FanlightLayoutAsset layout)
        {
            if (layout == null) return;

            var indices = new int[layout.BlockCount];
            for (var i = 0; i < indices.Length; i++) indices[i] = i;

            SetIndices(layout, indices);
        }

        internal static void Clear(FanlightLayoutAsset layout)
        {
            if (layout == null) return;

            SelectedBlockIds.Remove(layout.LayoutId.Value);
            NotifyChanged();
        }

        private static int FindBlockIndex(FanlightLayoutAsset layout, string blockId)
        {
            for (var i = 0; i < layout.BlockCount; i++)
            {
                if (string.Equals(layout.GetBlock(i).BlockId, blockId, StringComparison.Ordinal)) return i;
            }

            return -1;
        }

        private static void NotifyChanged()
        {
            Changed?.Invoke();
            SceneView.RepaintAll();
        }
    }
}
