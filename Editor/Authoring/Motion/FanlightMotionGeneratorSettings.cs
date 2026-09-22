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
        private float _sasageLowElevation = 18f;

        [SerializeField]
        private float _sasageHighElevation = 68f;

        [SerializeField]
        private float _sasageLowExtension = 0.7f;

        [SerializeField]
        private float _sasageHighExtension = 0.96f;

        [SerializeField]
        private float _sasageHoldRatio = 0.4f;

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

        internal float SasageLowElevation
        {
            get => _sasageLowElevation;
            set => _sasageLowElevation = value;
        }

        internal float SasageHighElevation
        {
            get => _sasageHighElevation;
            set => _sasageHighElevation = value;
        }

        internal float SasageLowExtension
        {
            get => _sasageLowExtension;
            set => _sasageLowExtension = value;
        }

        internal float SasageHighExtension
        {
            get => _sasageHighExtension;
            set => _sasageHighExtension = value;
        }

        internal float SasageHoldRatio
        {
            get => _sasageHoldRatio;
            set => _sasageHoldRatio = value;
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
