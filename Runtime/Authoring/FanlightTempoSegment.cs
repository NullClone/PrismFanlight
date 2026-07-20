using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal struct FanlightTempoSegment
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

        internal double StartSeconds => _startSeconds;

        internal double StartBeat => _startBeat;

        internal double Bpm => _bpm;

        internal int BeatsPerBar => _beatsPerBar;

        internal int BeatUnit => _beatUnit;

        internal long StartBar => _startBar;

        internal bool IsValid =>
            !double.IsNaN(StartSeconds) && !double.IsInfinity(StartSeconds)
                                        && !double.IsNaN(StartBeat) && !double.IsInfinity(StartBeat)
                                        && Bpm > 0d && !double.IsInfinity(Bpm)
                                        && BeatsPerBar >= 1
                                        && BeatUnit is 1 or 2 or 4 or 8 or 16;


        // Methods

        internal FanlightTempoSegment(
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
