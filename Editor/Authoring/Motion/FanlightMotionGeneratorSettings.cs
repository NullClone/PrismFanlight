using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [Serializable]
    internal sealed class FanlightMotionGeneratorSettings
    {
        // Fields

        [SerializeField]
        private int _preset = 0;

        [SerializeField]
        private DrumParameters _drumParams = DrumParameters.CreateDefault();

        [SerializeField]
        private WiperParameters _wiperParams = WiperParameters.CreateDefault();

        [SerializeField]
        private SasageParameters _sasageParams = SasageParameters.CreateDefault();

        [SerializeField]
        private float _generatorIntensity = 1f;

        [SerializeField]
        private bool _circleClockwise = true;


        // Properties

        internal int Preset
        {
            get => _preset;
            set => _preset = value;
        }

        internal DrumParameters DrumParams
        {
            get => _drumParams;
            set => _drumParams = value;
        }

        internal WiperParameters WiperParams
        {
            get => _wiperParams;
            set => _wiperParams = value;
        }

        internal SasageParameters SasageParams
        {
            get => _sasageParams;
            set => _sasageParams = value;
        }

        internal float GeneratorIntensity
        {
            get => _generatorIntensity;
            set => _generatorIntensity = value;
        }

        internal bool CircleClockwise
        {
            get => _circleClockwise;
            set => _circleClockwise = value;
        }
    }
}
