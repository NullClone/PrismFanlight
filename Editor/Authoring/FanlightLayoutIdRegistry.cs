using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    [InitializeOnLoad]
    internal static class FanlightLayoutIdRegistry
    {
        // Fields

        private static readonly Dictionary<string, int> Counts = new(StringComparer.Ordinal);
        private static bool _valid;


        // Methods

        static FanlightLayoutIdRegistry()
        {
            EditorApplication.projectChanged += Invalidate;
            EditorApplication.playModeStateChanged += _ => ScheduleSceneValidation();

            ScheduleSceneValidation();
        }

        internal static void Invalidate()
        {
            _valid = false;

            ScheduleSceneValidation();
        }

        internal static bool IsDuplicate(FanlightLayoutAsset layout)
        {
            if (layout == null || !layout.LayoutId.IsValid) return true;

            EnsureBuilt();

            return Counts.TryGetValue(layout.LayoutId.Value, out var count) && count > 1;
        }

        private static void EnsureBuilt()
        {
            if (_valid) return;

            Counts.Clear();

            foreach (var guid in AssetDatabase.FindAssets("t:FanlightLayoutAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var layout = AssetDatabase.LoadAssetAtPath<FanlightLayoutAsset>(path);

                if (layout == null || !layout.LayoutId.IsValid) continue;

                Counts.TryGetValue(layout.LayoutId.Value, out var count);
                Counts[layout.LayoutId.Value] = count + 1;
            }

            _valid = true;
        }

        private static void ScheduleSceneValidation()
        {
            EditorApplication.delayCall -= ValidateSceneInstances;
            EditorApplication.delayCall += ValidateSceneInstances;
        }

        private static void ValidateSceneInstances()
        {
            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                var layout = fanlight.LayoutAsset;
                fanlight.SetEditorLayoutBlocked(layout != null && layout.IsInitialized && IsDuplicate(layout));
            }
        }
    }
}
