using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [Serializable]
    internal sealed class FanlightMotionGeneratorSettings
    {
        // Fields

        [SerializeField]
        private int _preset;

        [SerializeField]
        private int _bakedPreset = -1;

        [SerializeField]
        private DrumParameters _drumParams = DrumParameters.CreateDefault();

        [SerializeField]
        private WiperParameters _wiperParams = WiperParameters.CreateDefault();

        [SerializeField]
        private SasageParameters _sasageParams = SasageParameters.CreateDefault();

        [SerializeField]
        private float _generatorIntensity = 1f;


        // Properties

        internal int Preset
        {
            get => _preset;
            set => _preset = value;
        }

        internal int BakedPreset
        {
            get => _bakedPreset;
            set => _bakedPreset = value;
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
    }
}
