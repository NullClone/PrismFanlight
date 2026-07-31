using System.Collections.Generic;
using PrismFanlight.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    internal static class FanlightControlBindingValidator
    {
        internal static void CollectErrors(TimelineAsset timeline, PlayableDirector director, PrismFanlight target, List<string> errors)
        {
            if (timeline == null || director == null || target == null) return;

            foreach (var outputTrack in timeline.GetOutputTracks())
            {
                if (outputTrack is not ControlTrack) continue;

                foreach (var clip in outputTrack.GetClips())
                {
                    if (clip.asset is not ControlPlayableAsset control || !control.updateDirector) continue;

                    if (control.prefabGameObject != null)
                    {
                        errors.Add($"Control Clip '{clip.displayName}' generates a Prefab and cannot define Fanlight Sequence ownership.");
                        continue;
                    }

                    var source = control.sourceGameObject.Resolve(director);
                    if (source == null) continue;

                    var controlledDirectors = control.searchHierarchy
                        ? source.GetComponentsInChildren<PlayableDirector>(true)
                        : source.GetComponents<PlayableDirector>();
                    var fanlightDirectorCount = 0;

                    for (var i = 0; i < controlledDirectors.Length; i++)
                    {
                        if (BindsTarget(controlledDirectors[i], target)) fanlightDirectorCount++;
                    }

                    if (fanlightDirectorCount == 0) continue;

                    if (controlledDirectors.Length != 1)
                    {
                        errors.Add($"Control Clip '{clip.displayName}' must resolve exactly one existing PlayableDirector for Fanlight Sequence ownership.");
                    }
                }
            }
        }

        private static bool BindsTarget(PlayableDirector director, PrismFanlight target)
        {
            if (director == null || director.playableAsset is not TimelineAsset timeline) return false;

            foreach (var outputTrack in timeline.GetOutputTracks())
            {
                if (outputTrack is not FanlightTimelineTrackAsset && outputTrack is not FanlightTempoTrack) continue;

                var binding = director.GetGenericBinding(outputTrack);

                if (binding == target || binding is GameObject gameObject && gameObject.GetComponent<PrismFanlight>() == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
