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
        private int _beatsPerBar = 4;

        [SerializeField]
        private FanlightBeatUnit _beatUnit = FanlightBeatUnit.u4;

        [SerializeField]
        private double _musicalOriginSeconds;


        // Properties

        internal int BeatsPerBar => _beatsPerBar;

        internal int BeatUnit => (int)_beatUnit;

        internal double MusicalOriginSeconds => _musicalOriginSeconds;

        // Methods

        public override bool CanCreateTrackMixer() => true;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            if (!TryBuildTempoSource(out var source, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var mixer = ScriptPlayable<FanlightTempoMixerBehaviour>.Create(graph, inputCount);
            var director = graph.GetResolver() as PlayableDirector;

            if (director == null)
            {
                throw new InvalidOperationException("Tempo Track requires a PlayableDirector graph resolver.");
            }

            mixer.GetBehaviour().Configure(source, director, this);
            return mixer;
        }


        internal bool TryBuildTempoSource(out FanlightTempoSource source, out string error)
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
                    source = null;
                    error = "A Timeline Asset can contain only one Fanlight Tempo Track.";
                    return false;
                }
            }

            return FanlightTempoDefinitionBuilder.TryBuildSource(
                BeatsPerBar,
                BeatUnit,
                MusicalOriginSeconds,
                GetClips(),
                out source,
                out error);
        }

        internal bool TryBuildRuntimeDefinition(
            FanlightTimeManager timeManager,
            out FanlightTempoRuntimeDefinition definition,
            out string error)
        {
            if (!TryBuildTempoSource(out var source, out error))
            {
                definition = null;
                return false;
            }

            if (!source.HasClips)
            {
                definition = null;
                error = string.Empty;
                return true;
            }

            if (timeManager == null)
            {
                definition = null;
                error = "Tempo Track requires a bound PrismFanlight with a Fanlight Time Manager.";
                return false;
            }

            return FanlightTempoDefinitionBuilder.TryBuildDefinition(
                source,
                timeManager.DefaultBpm,
                out definition,
                out error);
        }
    }
}
