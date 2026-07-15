using System;
using System.Collections.Generic;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    [InitializeOnLoad]
    internal static class FanlightTimelineEditorBridge
    {
        // Fields

        private const double TimeEpsilon = 0.000001;

        private static readonly HashSet<PrismFanlight> _targets = new();
        private static PlayableDirector _director;
        private static PlayableAsset _playableAsset;
        private static double _time;
        private static bool _hasTime;


        // Methods

        static FanlightTimelineEditorBridge()
        {
            EditorApplication.update += Update;
            PrismFanlight.ResolvedStateOverrideChanged += OnResolvedStateOverrideChanged;
        }


        private static void Update()
        {
            if (Application.isPlaying)
            {
                Reset();

                return;
            }

            var director = TimelineEditor.inspectedDirector;

            if (_director != director || _playableAsset != director?.playableAsset)
            {
                ClearTargets();

                _director = director;
                _playableAsset = director != null ? director.playableAsset : null;
                _hasTime = false;
            }

            if (_director == null || _director.playableAsset == null) return;

            var time = _director.time;

            if (_hasTime && Math.Abs(time - _time) <= TimeEpsilon) return;

            _time = time;
            _hasTime = true;

            if (_director.state == PlayState.Playing) return;

            _director.Evaluate();

            RequestRender();
        }

        private static void OnResolvedStateOverrideChanged(PrismFanlight fanlight)
        {
            if (Application.isPlaying || !IsBoundToInspectedDirector(fanlight)) return;

            if (fanlight.HasResolvedStateOverride)
            {
                _targets.Add(fanlight);
            }
            else
            {
                _targets.Remove(fanlight);
            }

            RequestRender();
        }

        private static bool IsBoundToInspectedDirector(PrismFanlight fanlight)
        {
            if (_director == null) return false;

            var timeline = _director.playableAsset as TimelineAsset;
            if (timeline == null) return false;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (IsFanlightTrack(track) && _director.GetGenericBinding(track) == fanlight)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFanlightTrack(TrackAsset track)
        {
            return track is FanlightTimelineTrack or FanlightPaletteGradientTrack;
        }

        private static void ClearTargets()
        {
            if (_targets.Count == 0) return;

            var targets = new PrismFanlight[_targets.Count];
            _targets.CopyTo(targets);
            _targets.Clear();

            foreach (var fanlight in targets)
            {
                if (fanlight != null)
                {
                    fanlight.ClearTimelineContributions();
                }
            }

            RequestRender();
        }

        private static void Reset()
        {
            ClearTargets();
            _director = null;
            _playableAsset = null;
            _hasTime = false;
        }

        private static void RequestRender()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
