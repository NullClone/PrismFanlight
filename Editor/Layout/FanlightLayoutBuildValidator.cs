using System.Collections.Generic;
using PrismFanlight.Authoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightLayoutBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var checkedPaths = new HashSet<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                foreach (var dependency in AssetDatabase.GetDependencies(scene.path, true))
                {
                    if (!checkedPaths.Add(dependency)) continue;
                    var layout = AssetDatabase.LoadAssetAtPath<FanlightLayoutAsset>(dependency);
                    if (layout == null) continue;
                    Validate(layout, dependency);
                }
            }
        }

        private static void Validate(FanlightLayoutAsset layout, string path)
        {
            if (!layout.IsInitialized)
            {
                throw new BuildFailedException($"Fanlight layout is not initialized: {path}");
            }

            if (FanlightLayoutIdRegistry.IsDuplicate(layout))
            {
                throw new BuildFailedException($"Fanlight layout ID is duplicated: {path}");
            }

            if (!layout.HasValidBake)
            {
                throw new BuildFailedException($"Fanlight layout requires a valid bake: {path}");
            }
        }
    }
}
