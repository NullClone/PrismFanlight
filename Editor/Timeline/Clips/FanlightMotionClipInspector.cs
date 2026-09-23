using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightMotionClip))]
    [CanEditMultipleObjects]
    internal sealed class FanlightMotionClipInspector : UnityEditor.Editor
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

            var includedFields = FanlightTimelineFieldMaskResolver.TryResolve(targets, out var mask, out _)
                ? mask.Motion
                : FanlightMotionFields.All;
            var sourceIncluded = includedFields.HasFlag(FanlightMotionFields.Source);
            var motionAsset = _value.FindPropertyRelative("_motionAsset");
            var beatsPerCycle = _value.FindPropertyRelative("_beatsPerCycle");

            using (new EditorGUI.DisabledScope(!sourceIncluded))
            {
                var previousAsset = motionAsset.objectReferenceValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(motionAsset);
                if (EditorGUI.EndChangeCheck()
                    && targets.Length == 1
                    && previousAsset == null
                    && motionAsset.objectReferenceValue is FanlightMotionAsset asset)
                {
                    beatsPerCycle.floatValue = FanlightMotionCycleDefaults.Suggest(asset);
                }

                EditorGUILayout.PropertyField(beatsPerCycle);
                EditorGUILayout.PropertyField(_value.FindPropertyRelative("_phaseOffsetBeats"));
            }

            DrawChild("_blockDelayXBeats", includedFields.HasFlag(FanlightMotionFields.BlockDelayXBeats));
            DrawChild("_blockDelayYBeats", includedFields.HasFlag(FanlightMotionFields.BlockDelayYBeats));
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChild(string propertyName, bool included)
        {
            using (new EditorGUI.DisabledScope(!included))
            {
                EditorGUILayout.PropertyField(_value.FindPropertyRelative(propertyName));
            }
        }
    }
}
