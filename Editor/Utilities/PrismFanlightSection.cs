using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightSectionExtensions
    {
        internal static void DrawSection(this PrismFanlightSection section, Action draw)
        {
            if (section.DrawHeader())
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    draw();

                    EditorGUILayout.Space();
                }
            }
        }

        internal static void DrawSection<TTrack>(this PrismFanlightSection<TTrack> section, Action draw, PrismFanlight fanlight) where TTrack : TrackAsset, new()
        {
            if (section.DrawHeader(fanlight))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    draw();

                    EditorGUILayout.Space();
                }
            }
        }
    }

    internal class PrismFanlightSection
    {
        // Fields

        protected readonly GUIContent title;

        protected bool expand;


        // Methods

        internal PrismFanlightSection(string title) : this(new GUIContent(title)) { }

        internal PrismFanlightSection(GUIContent title)
        {
            this.title = title;
        }

        internal bool DrawHeader()
        {
            CoreEditorUtils.DrawSplitter();

            expand = CoreEditorUtils.DrawHeaderFoldout(
                title: title,
                state: expand,
                documentationURL: PrismFanlight.HelpUrl);

            return expand;
        }
    }

    internal class PrismFanlightSection<TTrack> : PrismFanlightSection where TTrack : TrackAsset, new()
    {
        internal PrismFanlightSection(string title) : base(new GUIContent(title)) { }

        internal bool DrawHeader(PrismFanlight fanlight)
        {
            CoreEditorUtils.DrawSplitter();

            expand = CoreEditorUtils.DrawHeaderFoldout(
                title: title,
                state: expand,
                documentationURL: PrismFanlight.HelpUrl,
                customMenuContextAction: menu => AddTimelineTrackMenuItem(menu, fanlight));

            return expand;
        }


        private static void AddTimelineTrackMenuItem(GenericMenu menu, PrismFanlight fanlight)
        {
            var content = new GUIContent("Add to Timeline");

            if (!CanAddTimelineTrack(fanlight))
            {
                menu.AddDisabledItem(content);
                return;
            }

            menu.AddItem(content, false, () => AddTimelineTrack(fanlight));
        }

        private static bool CanAddTimelineTrack(PrismFanlight fanlight)
        {
            if (fanlight == null) return false;

            var timeline = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;

            if (timeline == null || director == null) return false;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not TTrack) continue;

                if (director.GetGenericBinding(track) == fanlight)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddTimelineTrack(PrismFanlight fanlight)
        {
            if (!CanAddTimelineTrack(fanlight)) return;

            var timeline = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;

            var undoName = $"Add {ObjectNames.NicifyVariableName(typeof(TTrack).Name)}";

            Undo.RegisterCompleteObjectUndo(timeline, undoName);
            Undo.RecordObject(director, undoName);

            var track = timeline.CreateTrack<TTrack>();

            Undo.RegisterCreatedObjectUndo(track, undoName);

            director.SetGenericBinding(track, fanlight);

            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(director);

            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }
    }
}
