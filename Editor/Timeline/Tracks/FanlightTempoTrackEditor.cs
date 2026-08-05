using System.Collections.Generic;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [CustomTimelineEditor(typeof(FanlightTempoTrack))]
    internal sealed class FanlightTempoTrackEditor : TrackEditor
    {
        // Methods

        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);

            if (track is not FanlightTempoTrack tempoTrack) return options;

            var errors = new List<string>();

            if (!tempoTrack.TryBuildRuntimeDefinition(out _, out var definitionError))
            {
                errors.Add(definitionError);
            }

            if (binding == null)
            {
                errors.Add("Tempo Track binding is missing. Bind it to the target PrismFanlight component.");
            }
            else if (binding is not PrismFanlight && binding is not GameObject)
            {
                errors.Add("Tempo Track binding has the wrong type.");
            }

            if (tempoTrack.timelineAsset != null && CountTempoTracks(tempoTrack.timelineAsset) > 1)
            {
                errors.Add("A Timeline Asset can contain only one Fanlight Tempo Track.");
            }

            var target = binding as PrismFanlight;
            if (target == null && binding is GameObject gameObject) target = gameObject.GetComponent<PrismFanlight>();

            if (target != null)
            {
                FanlightControlBindingValidator.CollectErrors(
                    tempoTrack.timelineAsset,
                    TimelineEditor.inspectedDirector,
                    target,
                    errors);
            }

            for (var i = 0; i < errors.Count; i++)
            {
                options.errorText = FanlightTimelineTrackEditor.AppendError(options.errorText, errors[i]);
            }

            return options;
        }

        private static int CountTempoTracks(TimelineAsset timeline)
        {
            var count = 0;

            foreach (var outputTrack in timeline.GetOutputTracks())
            {
                if (outputTrack is FanlightTempoTrack) count++;
            }

            return count;
        }
    }
}
