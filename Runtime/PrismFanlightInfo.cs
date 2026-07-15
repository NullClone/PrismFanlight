using UnityEditor.Callbacks;

namespace PrismFanlight
{
    public static class PrismFanlightInfo
    {
        public static string Name => "Prism Fanlight";

        public static string Version { get; private set; }


        [DidReloadScripts]
        private static void Initialize()
        {
            var assembly = typeof(PrismFanlightInfo).Assembly;
            var version = assembly.GetName().Version.ToString();
            var index = version.LastIndexOf('.');
            Version = version[..index];
        }
    }
}
