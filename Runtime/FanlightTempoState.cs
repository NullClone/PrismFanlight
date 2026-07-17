using PrismFanlight.Core;
using Unity.Mathematics;

namespace PrismFanlight
{
    public readonly struct FanlightTempoState
    {
        // Properties

        public bool Enable { get; }

        public float SongTime { get; }

        public float Bpm { get; }

        public int BeatsPerBar { get; }

        public float Beat { get; }

        public float BeatPhase { get; }

        public float BarPhase { get; }


        // Methods

        private FanlightTempoState(bool enable, float songTime, float bpm, int beatsPerBar, float beat, float beatPhase, float barPhase)
        {
            Enable = enable;
            SongTime = songTime;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            Beat = beat;
            BeatPhase = beatPhase;
            BarPhase = barPhase;
        }

        public static FanlightTempoState FromSongTime(bool enable, float songTime, float bpm, int beatsPerBar)
        {
            var validatedBpm = math.max(1.0f, bpm);
            var validatedBeatsPerBar = math.max(1, beatsPerBar);
            var beat = math.max(0.0f, songTime) * validatedBpm / 60.0f;
            var beatPhase = math.frac(beat);
            var barPhase = math.frac(beat / validatedBeatsPerBar);

            return new FanlightTempoState(
                enable,
                songTime,
                validatedBpm,
                validatedBeatsPerBar,
                beat,
                beatPhase,
                barPhase);
        }

        public static FanlightTempoState FromMusicalPosition(bool enable, FanlightMusicalPosition position)
        {
            return new FanlightTempoState(
                enable,
                (float)position.Seconds,
                (float)position.Bpm,
                position.BeatsPerBar,
                (float)position.Beat,
                (float)position.BeatPhase,
                (float)position.BarPhase);
        }
    }
}
