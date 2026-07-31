using System;
using PrismFanlight.Core;
using PrismFanlight.Time;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    [TrackClipType(typeof(FanlightTempoClip))]
    [TrackBindingType(typeof(PrismFanlight))]
    [TrackColor(0.18f, 0.55f, 0.85f)]
    public sealed class FanlightTempoTrack : TrackAsset
    {
        // Fields

        [SerializeField]
        private double _defaultBpm = 120d;

        [SerializeField]
        private int _beatsPerBar = 4;

        [SerializeField]
        private FanlightBeatUnit _beatUnit = FanlightBeatUnit.u4;

        [SerializeField]
        private double _musicalOriginSeconds;


        // Properties

        internal double DefaultBpm => _defaultBpm;

        internal int BeatsPerBar => _beatsPerBar;

        internal int BeatUnit => (int)_beatUnit;

        internal double MusicalOriginSeconds => _musicalOriginSeconds;


        // Methods

        public override bool CanCreateTrackMixer() => true;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            if (!TryBuildRuntimeDefinition(out var definition, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var mixer = ScriptPlayable<FanlightTempoMixerBehaviour>.Create(graph, inputCount);
            mixer.GetBehaviour().Configure(definition);
            return mixer;
        }


        internal bool TryBuildRuntimeDefinition(out FanlightTempoRuntimeDefinition definition, out string error)
        {
            if (timelineAsset != null)
            {
                var tempoTrackCount = 0;

                foreach (var outputTrack in timelineAsset.GetOutputTracks())
                {
                    if (outputTrack is FanlightTempoTrack) tempoTrackCount++;
                }

                if (tempoTrackCount > 1)
                {
                    definition = null;
                    error = "A Timeline Asset can contain only one Fanlight Tempo Track.";
                    return false;
                }
            }

            return FanlightTempoDefinitionBuilder.TryBuild(
                DefaultBpm,
                BeatsPerBar,
                BeatUnit,
                MusicalOriginSeconds,
                GetClips(),
                out definition,
                out error);
        }
    }
}
