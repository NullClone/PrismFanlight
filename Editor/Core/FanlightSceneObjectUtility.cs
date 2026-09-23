using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightSceneObjectUtility
    {
        internal static PrismFanlight[] FindFanlights(FindObjectsInactive inactive = FindObjectsInactive.Exclude)
        {
#if UNITY_6000_5_OR_NEWER
            return Object.FindObjectsByType<PrismFanlight>(inactive);
#else
            return Object.FindObjectsByType<PrismFanlight>(inactive, FindObjectsSortMode.None);
#endif
        }
    }
}
