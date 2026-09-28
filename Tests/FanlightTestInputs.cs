using System;
using PrismFanlight.Core;
using PrismFanlight.Time;

namespace PrismFanlight.Tests
{
    internal static class FanlightTestInputs
    {
        // Fields

        internal const double Bpm = 120d;


        // Methods

        internal static FanlightShowState BaseState(float energy = 1f, float participation = 0.5f)
        {
            return new FanlightShowState(
                new FanlightIntentState(energy, participation, 0.5f, 0.5f),
                FanlightShowStateDefaults.Motion(),
                FanlightShowStateDefaults.Variation(),
                FanlightShowStateDefaults.Noise(),
                FanlightShowStateDefaults.Rest(),
                FanlightShowStateDefaults.AudienceBody(),
                FanlightShowStateDefaults.Direction(),
                FanlightShowStateDefaults.Color(),
                FanlightShowStateDefaults.Intensity(),
                FanlightShowStateDefaults.Visibility(),
                1u);
        }

        internal static FanlightShowPatch IntentPatch(FanlightIntentFields fields, float energy, float participation = 0.5f)
        {
            return new FanlightShowPatch(
                new FanlightIntentPatch(fields, new FanlightIntentState(energy, participation, 0.5f, 0.5f)),
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default);
        }

        internal static FanlightShowContribution Contribution(
            FanlightSequenceContext context,
            int trackPriority,
            int trackOrder,
            FanlightShowPatch patch,
            float weight = 1f,
            double startSeconds = 0d,
            double endSeconds = 10d)
        {
            return new FanlightShowContribution(context, trackPriority, trackOrder, startSeconds, endSeconds, weight, patch);
        }

        internal static FanlightShowTimeSample AtSeconds(
            double seconds,
            FanlightTimeDiscontinuity discontinuity = FanlightTimeDiscontinuity.None)
        {
            return AtBeat(seconds * Bpm / 60d, 4, discontinuity);
        }

        internal static FanlightShowTimeSample AtBeat(
            double beat,
            int beatsPerBar = 4,
            FanlightTimeDiscontinuity discontinuity = FanlightTimeDiscontinuity.None)
        {
            var seconds = beat * 60d / Bpm;
            var completedBars = Math.Floor(beat / beatsPerBar);
            var beatInBar = beat - completedBars * beatsPerBar;
            var position = new FanlightMusicalPosition(
                seconds,
                beat,
                (long)completedBars + 1L,
                beatInBar,
                beat - Math.Floor(beat),
                beatInBar / beatsPerBar,
                Bpm,
                beatsPerBar,
                4);

            return new FanlightShowTimeSample(seconds, 1d, FanlightClockStatus.Ready, discontinuity, false, true, position);
        }

        internal static FanlightClockSample Clock(
            double seconds,
            double rate = 1d,
            FanlightClockStatus status = FanlightClockStatus.Ready)
        {
            return new FanlightClockSample(seconds, rate, status, FanlightTimeDiscontinuity.None, false, true);
        }
    }
}
