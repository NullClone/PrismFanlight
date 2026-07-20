using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    public struct FanlightTempoSegment
    {
        // Fields

        [SerializeField]
        private double _startSeconds;

        [SerializeField]
        private double _startBeat;

        [SerializeField]
        private double _bpm;

        [SerializeField]
        private int _beatsPerBar;

        [SerializeField]
        private int _beatUnit;

        [SerializeField]
        private long _startBar;


        // Properties

        public double StartSeconds => _startSeconds;

        public double StartBeat => _startBeat;

        public double Bpm => _bpm;

        public int BeatsPerBar => _beatsPerBar;

        public int BeatUnit => _beatUnit;

        public long StartBar => _startBar;

        public bool IsValid =>
            !double.IsNaN(StartSeconds) && !double.IsInfinity(StartSeconds)
                                        && !double.IsNaN(StartBeat) && !double.IsInfinity(StartBeat)
                                        && Bpm > 0d && !double.IsInfinity(Bpm)
                                        && BeatsPerBar >= 1
                                        && BeatUnit is 1 or 2 or 4 or 8 or 16;


        // Methods

        public FanlightTempoSegment(
            double startSeconds,
            double startBeat,
            double bpm,
            int beatsPerBar,
            int beatUnit,
            long startBar)
        {
            _startSeconds = startSeconds;
            _startBeat = startBeat;
            _bpm = bpm;
            _beatsPerBar = beatsPerBar;
            _beatUnit = beatUnit;
            _startBar = startBar;
        }
    }
}
