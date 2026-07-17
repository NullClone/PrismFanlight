namespace PrismFanlight
{
    public static class PrismFanlightInfo
    {
        public static string Name => "Prism Fanlight";

        public static string Version { get; } = ResolveVersion();

        private static string ResolveVersion()
        {
            var assembly = typeof(PrismFanlightInfo).Assembly;
            var version = assembly.GetName().Version.ToString();
            var index = version.LastIndexOf('.');
            return index > 0 ? version[..index] : version;
        }
    }
}
