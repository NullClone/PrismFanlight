using PrismFanlight.Authoring;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightIntensityClip))]
    [CanEditMultipleObjects]
    internal sealed class FanlightIntensityClipInspector : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _value;


        // Methods

        private void OnEnable()
        {
            _value = serializedObject.FindProperty(nameof(_value));
        }

        public override void OnInspectorGUI()
        {
            FanlightPresetEditor.Draw(targets);

            serializedObject.Update();

            FanlightColorIntensityEditorUtility.DrawIntensityState(_value, ResolveLayout());

            serializedObject.ApplyModifiedProperties();
        }

        private FanlightLayoutAsset ResolveLayout()
        {
            if (targets.Length != 1
                || TimelineEditor.inspectedAsset == null
                || TimelineEditor.inspectedDirector == null)
            {
                return null;
            }

            foreach (var track in TimelineEditor.inspectedAsset.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset != target) continue;

                    var binding = TimelineEditor.inspectedDirector.GetGenericBinding(track);
                    var fanlight = binding as PrismFanlight;
                    if (fanlight == null && binding is GameObject gameObject)
                    {
                        fanlight = gameObject.GetComponent<PrismFanlight>();
                    }

                    return fanlight != null ? fanlight.LayoutAsset : null;
                }
            }

            return null;
        }
    }
}
