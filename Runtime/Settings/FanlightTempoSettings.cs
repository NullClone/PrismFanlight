using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    public enum FanlightTempoClockSource
    {
        UnityTime,
        AudioSourceTime,
        ManualTime
    }

    [Serializable]
    public struct FanlightTempoSettings
    {
        // Fields

        [Min(1.0f)]
        public float bpm;

        [Min(1)]
        public int beatsPerBar;

        public float offsetSeconds;

        public float latencyCompensationSeconds;

        public FanlightTempoClockSource clockSource;

        public AudioSource audioSource;

        [Min(0.0f)]
        public float manualTime;


        // Methods

        public static FanlightTempoSettings Default() => new()
        {
            bpm = 120.0f,
            beatsPerBar = 4,
            offsetSeconds = 0.0f,
            latencyCompensationSeconds = 0.0f,
            clockSource = FanlightTempoClockSource.UnityTime,
            audioSource = null,
            manualTime = 0.0f
        };

        public FanlightTempoSettings Validated() => new()
        {
            bpm = math.max(1.0f, bpm),
            beatsPerBar = math.max(1, beatsPerBar),
            offsetSeconds = offsetSeconds,
            latencyCompensationSeconds = latencyCompensationSeconds,
            clockSource = clockSource,
            audioSource = audioSource,
            manualTime = math.max(0.0f, manualTime)
        };

        public FanlightTempoState Evaluate(float unityTime)
        {
            var settings = Validated();
            var enable = true;
            var rawTime = settings.clockSource switch
            {
                FanlightTempoClockSource.AudioSourceTime => GetAudioSourceTime(settings.audioSource, unityTime, out enable),
                FanlightTempoClockSource.ManualTime => settings.manualTime,
                _ => unityTime
            };

            var songTime = math.max(0.0f, rawTime - settings.offsetSeconds + settings.latencyCompensationSeconds);

            return FanlightTempoState.FromSongTime(enable, songTime, settings.bpm, settings.beatsPerBar);
        }

        private static float GetAudioSourceTime(AudioSource source, float fallbackTime, out bool enable)
        {
            if (source == null || source.clip == null)
            {
                enable = false;
                return fallbackTime;
            }

            enable = true;

            return source.clip.frequency > 0
                ? (float)source.timeSamples / source.clip.frequency
                : source.time;
        }
    }

    public readonly struct FanlightTempoState
    {
        public bool Enable { get; }

        public float SongTime { get; }

        public float Bpm { get; }

        public int BeatsPerBar { get; }

        public float Beat { get; }

        public float BeatPhase { get; }

        public float BarPhase { get; }


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
    }
}
