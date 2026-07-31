using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    internal static class FanlightSequenceContextRegistry
    {
        // Fields

        private static readonly Dictionary<PlayableDirector, int> ActiveReferences = new();
        private static readonly Dictionary<PlayableDirector, PlayableDirector> Parents = new();
        private static readonly Dictionary<PlayableDirector, FanlightSequenceContext> Contexts = new();

        private static PlayableDirector[] _knownDirectors = Array.Empty<PlayableDirector>();


        // Methods

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ReleaseContexts();

            ActiveReferences.Clear();
            Parents.Clear();
            _knownDirectors = Array.Empty<PlayableDirector>();
        }

        internal static void Acquire(PlayableDirector director)
        {
            if (director == null)
            {
                throw new InvalidOperationException("A PlayableDirector graph resolver is required for Sequence Context.");
            }

            ActiveReferences.TryGetValue(director, out var count);
            ActiveReferences[director] = count + 1;

            CaptureKnownDirectors();
            RefreshRelationships();
        }

        internal static void Release(PlayableDirector director)
        {
            if (director == null || !ActiveReferences.TryGetValue(director, out var count)) return;

            if (count > 1)
            {
                ActiveReferences[director] = count - 1;
            }
            else
            {
                ActiveReferences.Remove(director);
            }

            RefreshRelationships();
        }

        internal static FanlightSequenceContext GetContext(PlayableDirector director)
        {
            if (director == null || !ActiveReferences.ContainsKey(director))
            {
                throw new InvalidOperationException("Sequence Context is not registered for this PlayableDirector evaluation.");
            }

            RefreshRelationships();

            if (!Contexts.TryGetValue(director, out var context) || context.IsReleased)
            {
                throw new InvalidOperationException("Sequence Context could not be resolved.");
            }

            return context;
        }

        private static void CaptureKnownDirectors()
        {
            var allDirectors = Resources.FindObjectsOfTypeAll<PlayableDirector>();
            var sceneDirectors = new List<PlayableDirector>(allDirectors.Length);

            for (var i = 0; i < allDirectors.Length; i++)
            {
                var director = allDirectors[i];

                if (director != null && director.gameObject.scene.IsValid())
                {
                    sceneDirectors.Add(director);
                }
            }

            _knownDirectors = sceneDirectors.ToArray();
        }

        private static void RefreshRelationships()
        {
            if (ActiveReferences.Count == 0)
            {
                ReleaseContexts();
                Parents.Clear();
                return;
            }

            var nextParents = new Dictionary<PlayableDirector, PlayableDirector>();
            var visiting = new HashSet<PlayableDirector>();

            foreach (var director in ActiveReferences.Keys)
            {
                ResolveParentChain(director, nextParents, visiting);
            }

            if (RelationshipsMatch(nextParents))
            {
                PruneUnusedContexts(nextParents);
                return;
            }

            ReleaseContexts();

            Parents.Clear();

            foreach (var pair in nextParents)
            {
                Parents.Add(pair.Key, pair.Value);
            }

            foreach (var director in ActiveReferences.Keys)
            {
                BuildContext(director, new HashSet<PlayableDirector>());
            }
        }

        private static void ResolveParentChain(
            PlayableDirector child,
            Dictionary<PlayableDirector, PlayableDirector> nextParents,
            HashSet<PlayableDirector> visiting)
        {
            if (!visiting.Add(child))
            {
                throw new InvalidOperationException("Fanlight Sequence Context contains a cycle.");
            }

            var parent = FindParent(child);

            if (parent != null)
            {
                nextParents[child] = parent;
                ResolveParentChain(parent, nextParents, visiting);
            }

            visiting.Remove(child);
        }

        private static PlayableDirector FindParent(PlayableDirector child)
        {
            PlayableDirector parent = null;

            for (var i = 0; i < _knownDirectors.Length; i++)
            {
                var candidate = _knownDirectors[i];
                if (candidate == null || candidate == child) continue;

                var resolution = ResolveControlledDirector(candidate, child);
                if (!resolution) continue;

                if (parent != null && parent != candidate)
                {
                    throw new InvalidOperationException("A Fanlight Sequence cannot have multiple direct parents.");
                }

                parent = candidate;
            }

            return parent;
        }

        private static bool ResolveControlledDirector(PlayableDirector parent, PlayableDirector child)
        {
            if (parent.playableAsset is not TimelineAsset timeline) return false;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not ControlTrack) continue;

                foreach (var clip in track.GetClips())
                {
                    if (clip.asset is not ControlPlayableAsset control || !control.updateDirector) continue;
                    if (control.prefabGameObject != null)
                    {
                        if (ActiveReferences.ContainsKey(parent))
                        {
                            throw new InvalidOperationException("Prefab-generating Control Clips cannot define Fanlight Sequence ownership.");
                        }

                        continue;
                    }

                    var source = control.sourceGameObject.Resolve(parent);
                    if (source == null) continue;

                    var directors = control.searchHierarchy
                        ? source.GetComponentsInChildren<PlayableDirector>(true)
                        : source.GetComponents<PlayableDirector>();

                    var containsChild = false;

                    for (var i = 0; i < directors.Length; i++)
                    {
                        if (directors[i] == child) containsChild = true;
                    }

                    if (!containsChild) continue;

                    if (directors.Length != 1)
                    {
                        throw new InvalidOperationException("A Fanlight Sequence Control binding must resolve exactly one existing PlayableDirector.");
                    }

                    return true;
                }
            }

            return false;
        }

        private static FanlightSequenceContext BuildContext(
            PlayableDirector director,
            HashSet<PlayableDirector> visiting)
        {
            if (Contexts.TryGetValue(director, out var existing)) return existing;

            if (!visiting.Add(director))
            {
                throw new InvalidOperationException("Fanlight Sequence Context contains a cycle.");
            }

            FanlightSequenceContext parentContext = null;

            if (Parents.TryGetValue(director, out var parent))
            {
                parentContext = BuildContext(parent, visiting);
            }

            var context = new FanlightSequenceContext(parentContext);
            Contexts.Add(director, context);
            visiting.Remove(director);
            return context;
        }

        private static bool RelationshipsMatch(Dictionary<PlayableDirector, PlayableDirector> nextParents)
        {
            if (nextParents.Count != Parents.Count) return false;

            foreach (var pair in nextParents)
            {
                if (!Parents.TryGetValue(pair.Key, out var parent) || parent != pair.Value) return false;
            }

            foreach (var director in ActiveReferences.Keys)
            {
                if (!Contexts.TryGetValue(director, out var context) || context.IsReleased) return false;
            }

            return true;
        }

        private static void PruneUnusedContexts(Dictionary<PlayableDirector, PlayableDirector> relationships)
        {
            var required = new HashSet<PlayableDirector>();

            foreach (var director in ActiveReferences.Keys)
            {
                required.Add(director);
            }

            foreach (var pair in relationships)
            {
                required.Add(pair.Key);
                required.Add(pair.Value);
            }

            var unused = new List<PlayableDirector>();

            foreach (var pair in Contexts)
            {
                if (!required.Contains(pair.Key)) unused.Add(pair.Key);
            }

            for (var i = 0; i < unused.Count; i++)
            {
                Contexts[unused[i]].Release();
                Contexts.Remove(unused[i]);
            }
        }

        private static void ReleaseContexts()
        {
            foreach (var context in Contexts.Values)
            {
                context.Release();
            }

            Contexts.Clear();
        }
    }
}
