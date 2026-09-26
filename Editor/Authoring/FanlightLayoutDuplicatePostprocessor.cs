using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEditor;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightLayoutDuplicatePostprocessor : AssetPostprocessor
    {
        // Fields

        private static readonly HashSet<string> PendingPaths = new(StringComparer.Ordinal);


        // Methods

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var queued = false;
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(FanlightLayoutAsset)) continue;

                queued |= PendingPaths.Add(path);
            }

            if (!queued) return;

            EditorApplication.delayCall -= ResolvePending;
            EditorApplication.delayCall += ResolvePending;
        }

        private static void ResolvePending()
        {
            if (PendingPaths.Count == 0) return;

            var imported = new HashSet<string>(PendingPaths, StringComparer.Ordinal);
            PendingPaths.Clear();

            var holders = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:FanlightLayoutAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var layout = AssetDatabase.LoadAssetAtPath<FanlightLayoutAsset>(path);
                if (layout == null || !layout.LayoutId.IsValid) continue;

                if (!holders.TryGetValue(layout.LayoutId.Value, out var paths))
                {
                    paths = new List<string>();
                    holders.Add(layout.LayoutId.Value, paths);
                }

                paths.Add(path);
            }

            foreach (var paths in holders.Values)
            {
                if (paths.Count < 2) continue;

                paths.Sort(StringComparer.Ordinal);
                var keeper = paths.Find(path => !imported.Contains(path)) ?? paths[0];

                foreach (var path in paths)
                {
                    if (path == keeper || !imported.Contains(path)) continue;

                    AssignNewLayoutId(AssetDatabase.LoadAssetAtPath<FanlightLayoutAsset>(path));
                }
            }
        }

        private static void AssignNewLayoutId(FanlightLayoutAsset layout)
        {
            if (layout == null) return;

            var rebake = layout.ActiveBake != null
                         && FanlightLayoutEditSession.IsEmbeddedBake(layout, layout.ActiveBake)
                         && layout.HasValidBake;

            layout.RegenerateLayoutId();
            EditorUtility.SetDirty(layout);
            FanlightLayoutEditSession.Reset(layout);
            FanlightLayoutIdRegistry.Invalidate();

            if (!rebake || FanlightLayoutEditSession.Get(layout)?.Bake() != true)
            {
                AssetDatabase.SaveAssetIfDirty(layout);
            }
        }
    }
}
