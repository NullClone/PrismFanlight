namespace PrismFanlight.Live
{
    public readonly struct FanlightLiveStatus
    {
        // Fields

        private readonly string _timeFault;
        private readonly string _sequenceFault;
        private readonly string _rendererFault;
        private readonly string _liveFault;


        // Properties

        public bool IsRendering { get; }

        public bool IsHolding { get; }

        public bool IsTimeFallbackActive { get; }

        public string TimeFault => _timeFault ?? string.Empty;

        public string SequenceFault => _sequenceFault ?? string.Empty;

        public string RendererFault => _rendererFault ?? string.Empty;

        public string LiveFault => _liveFault ?? string.Empty;

        public bool HasFault =>
            !string.IsNullOrEmpty(_timeFault)
            || !string.IsNullOrEmpty(_sequenceFault)
            || !string.IsNullOrEmpty(_rendererFault)
            || !string.IsNullOrEmpty(_liveFault);


        // Methods

        internal FanlightLiveStatus(
            bool isRendering,
            bool isHolding,
            bool isTimeFallbackActive,
            string timeFault,
            string sequenceFault,
            string rendererFault,
            string liveFault)
        {
            IsRendering = isRendering;
            IsHolding = isHolding;
            IsTimeFallbackActive = isTimeFallbackActive;
            _timeFault = timeFault;
            _sequenceFault = sequenceFault;
            _rendererFault = rendererFault;
            _liveFault = liveFault;
        }
    }
}
