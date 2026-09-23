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
            FanlightTempoSource source = null;

            if (!tempoTrack.TryBuildTempoSource(out source, out var sourceError))
            {
                errors.Add(sourceError);
            }

            var target = ResolveTarget(binding);

            if (source is { HasClips: true } && binding == null)
            {
                errors.Add("Tempo Track binding is missing. Bind it to the target PrismFanlight component.");
            }
            else if (source is { HasClips: true } && target == null)
            {
                errors.Add("Tempo Track binding has the wrong type.");
            }
            else if (source is { HasClips: true } && target.TimeManager == null)
            {
                errors.Add("Tempo Track requires the bound PrismFanlight to reference a Fanlight Time Manager.");
            }
            else if (source is { HasClips: true }
                     && !tempoTrack.TryBuildRuntimeDefinition(target.TimeManager, out _, out var definitionError))
            {
                errors.Add(definitionError);
            }

            if (tempoTrack.timelineAsset != null && CountTempoTracks(tempoTrack.timelineAsset) > 1)
            {
                errors.Add("A Timeline Asset can contain only one Fanlight Tempo Track.");
            }

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

        private static PrismFanlight ResolveTarget(Object binding)
        {
            if (binding is PrismFanlight target) return target;
            return binding is GameObject gameObject ? gameObject.GetComponent<PrismFanlight>() : null;
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
