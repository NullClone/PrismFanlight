using PrismFanlight.Time;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightMenuItem
    {
        [MenuItem("GameObject/Light/Prism Fanlight", false, 31)]
        private static void CreatePrismFanlight(MenuCommand command)
        {
            var obj = new GameObject("Prism Fanlight", typeof(PrismFanlight), typeof(FanlightTimeManager));

            GameObjectUtility.SetParentAndAlign(obj, command.context as GameObject);

            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);

            obj.GetComponent<PrismFanlight>().SetTimeManager(obj.GetComponent<FanlightTimeManager>());

            Selection.activeObject = obj;
        }
    }
}
