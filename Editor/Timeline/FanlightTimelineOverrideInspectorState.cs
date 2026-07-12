using System.Collections.Generic;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    internal static class FanlightTimelineOverrideInspectorState
    {
        private static readonly HashSet<string> _activePaths = new();
        private static PrismFanlight _target;
        private static PlayableDirector _director;
        private static double _time;


        public static void Update(PrismFanlight target)
        {
            _activePaths.Clear();
            _target = target;
            _director = TimelineEditor.inspectedDirector;

            if (target == null || _director == null) return;
            if (_director.playableAsset is not TimelineAsset timeline) return;

            _time = _director.time;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not FanlightTimelineTrack) continue;
                if (track.mutedInHierarchy) continue;
                if (_director.GetGenericBinding(track) != target) continue;

                AddActiveClipPaths(track, _time);
            }
        }

        public static bool IsOverridden(string serializedPropertyPath)
        {
            if (_target == null || _director == null) return false;

            var timelinePath = ToTimelinePath(serializedPropertyPath);
            return timelinePath != null && _activePaths.Contains(timelinePath);
        }

        public static bool IsInspecting(PrismFanlight target)
        {
            var director = TimelineEditor.inspectedDirector;
            if (target == null || director == null) return false;
            if (director.playableAsset is not TimelineAsset timeline) return false;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is FanlightTimelineTrack && director.GetGenericBinding(track) == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddActiveClipPaths(TrackAsset track, double time)
        {
            foreach (var clip in track.GetClips())
            {
                if (time < clip.start || time >= clip.end) continue;
                if (clip.asset is not FanlightTimelinePlayableAsset asset) continue;

                foreach (var path in asset.OverridePaths)
                {
                    _activePaths.Add(path);
                }
            }
        }

        private static string ToTimelinePath(string serializedPropertyPath)
        {
            const string color = "_color.";
            const string motion = "_motion.";
            const string tempo = "_tempo.";
            const string audience = "_audienceSettings.";

            if (serializedPropertyPath.StartsWith(color)) return $"color.{serializedPropertyPath[color.Length..]}";
            if (serializedPropertyPath.StartsWith(motion)) return $"motion.{serializedPropertyPath[motion.Length..]}";
            if (serializedPropertyPath.StartsWith(tempo)) return $"tempo.{serializedPropertyPath[tempo.Length..]}";
            if (serializedPropertyPath.StartsWith(audience)) return $"audience.{serializedPropertyPath[audience.Length..]}";

            return null;
        }
    }
}
