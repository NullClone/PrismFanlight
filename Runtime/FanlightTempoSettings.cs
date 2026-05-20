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

        public bool enabled;

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
            enabled = false,
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
            enabled = enabled,
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
            var clockReady = true;
            var rawTime = settings.clockSource switch
            {
                FanlightTempoClockSource.AudioSourceTime => GetAudioSourceTime(settings.audioSource, unityTime, out clockReady),
                FanlightTempoClockSource.ManualTime => settings.manualTime,
                _ => unityTime
            };

            var songTime = math.max(0.0f, rawTime - settings.offsetSeconds + settings.latencyCompensationSeconds);
            return FanlightTempoState.FromSongTime(settings.enabled && clockReady, songTime, settings.bpm, settings.beatsPerBar, clockReady);
        }

        private static float GetAudioSourceTime(AudioSource source, float fallbackTime, out bool clockReady)
        {
            if (source == null || source.clip == null)
            {
                clockReady = false;
                return fallbackTime;
            }

            clockReady = true;
            return source.clip.frequency > 0
                ? (float)source.timeSamples / source.clip.frequency
                : source.time;
        }
    }

    public readonly struct FanlightTempoState
    {
        public bool Enabled { get; }

        public bool ClockReady { get; }

        public float SongTime { get; }

        public float Bpm { get; }

        public int BeatsPerBar { get; }

        public float Beat { get; }

        public float BeatPhase { get; }

        public float BarPhase { get; }


        public FanlightTempoState(bool enabled, bool clockReady, float songTime, float bpm, int beatsPerBar, float beat, float beatPhase, float barPhase)
        {
            Enabled = enabled;
            ClockReady = clockReady;
            SongTime = songTime;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            Beat = beat;
            BeatPhase = beatPhase;
            BarPhase = barPhase;
        }

        public static FanlightTempoState Disabled(float time)
        {
            return FromSongTime(false, time, 120.0f, 4);
        }

        public static FanlightTempoState FromSongTime(bool enabled, float songTime, float bpm, int beatsPerBar)
        {
            return FromSongTime(enabled, songTime, bpm, beatsPerBar, true);
        }

        public static FanlightTempoState FromSongTime(bool enabled, float songTime, float bpm, int beatsPerBar, bool clockReady)
        {
            var validatedBpm = math.max(1.0f, bpm);
            var validatedBeatsPerBar = math.max(1, beatsPerBar);
            var beat = math.max(0.0f, songTime) * validatedBpm / 60.0f;
            var beatPhase = math.frac(beat);
            var barPhase = math.frac(beat / validatedBeatsPerBar);

            return new FanlightTempoState(
                enabled,
                clockReady,
                songTime,
                validatedBpm,
                validatedBeatsPerBar,
                beat,
                beatPhase,
                barPhase);
        }
    }
}
