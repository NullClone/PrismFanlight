using System;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    internal sealed class FanlightLiveLookQueue
    {
        // Fields

        private const double BoundaryTolerance = 1e-6d;

        private FanlightLiveLook _look;
        private FanlightLiveQuantize _quantize;
        private bool _hasTarget;
        private double _targetBeat;
        private double _anchorBeat;


        // Properties

        internal FanlightLiveLook PendingLook => _look;

        internal FanlightLiveQuantize PendingQuantize => _quantize;


        // Methods

        internal void Request(FanlightLiveLook look, FanlightLiveQuantize quantize)
        {
            _look = look ?? throw new ArgumentNullException(nameof(look));
            _quantize = quantize;
            _hasTarget = false;
        }

        internal void Clear()
        {
            _look = null;
            _hasTarget = false;
        }

        internal bool TryTakeDue(in FanlightShowTimeSample time, out FanlightLiveLook look)
        {
            look = null;

            if (_look == null) return false;

            if (_quantize != FanlightLiveQuantize.Immediate)
            {
                var position = time.MusicalPosition;
                var beat = position.Beat;

                if (!_hasTarget || beat < _anchorBeat - BoundaryTolerance)
                {
                    _targetBeat = ResolveTargetBeat(position);
                    _anchorBeat = beat;
                    _hasTarget = true;
                }

                if (beat < _targetBeat - BoundaryTolerance) return false;
            }

            look = _look;

            Clear();

            return true;
        }

        private double ResolveTargetBeat(in FanlightMusicalPosition position)
        {
            var beat = position.Beat;

            if (_quantize == FanlightLiveQuantize.Bar)
            {
                return position.BeatInBar <= BoundaryTolerance
                    ? beat
                    : beat - position.BeatInBar + position.BeatsPerBar;
            }

            var previousBoundary = Math.Floor(beat);
            return beat - previousBoundary <= BoundaryTolerance ? beat : previousBoundary + 1d;
        }
    }
}
