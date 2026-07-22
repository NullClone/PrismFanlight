using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightMotionAsset))]
    internal sealed class FanlightMotionAssetEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _armElevation;
        private SerializedProperty _armSide;
        private SerializedProperty _armExtension;
        private SerializedProperty _penlightElevation;
        private SerializedProperty _penlightSide;
        private SerializedProperty _bodyLean;
        private float _phase;


        // Methods

        private void OnEnable()
        {
            _armElevation = serializedObject.FindProperty(nameof(_armElevation));
            _armSide = serializedObject.FindProperty(nameof(_armSide));
            _armExtension = serializedObject.FindProperty(nameof(_armExtension));
            _penlightElevation = serializedObject.FindProperty(nameof(_penlightElevation));
            _penlightSide = serializedObject.FindProperty(nameof(_penlightSide));
            _bodyLean = serializedObject.FindProperty(nameof(_bodyLean));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Motion Curves", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_armElevation, new GUIContent("Arm Elevation"));
            EditorGUILayout.PropertyField(_armSide, new GUIContent("Arm Side"));
            EditorGUILayout.PropertyField(_armExtension, new GUIContent("Arm Extension"));
            EditorGUILayout.PropertyField(_penlightElevation, new GUIContent("Penlight Elevation"));
            EditorGUILayout.PropertyField(_penlightSide, new GUIContent("Penlight Side"));
            EditorGUILayout.PropertyField(_bodyLean, new GUIContent("Body Lean"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pose At Phase", EditorStyles.boldLabel);
            _phase = EditorGUILayout.Slider("Cycle Phase", _phase, 0f, 1f);
            DrawPhaseValue(_armElevation, "Arm Elevation", -90f, 90f);
            DrawPhaseValue(_armSide, "Arm Side", -90f, 90f);
            DrawPhaseValue(_armExtension, "Arm Extension", 0f, 1f);
            DrawPhaseValue(_penlightElevation, "Penlight Elevation", -90f, 90f);
            DrawPhaseValue(_penlightSide, "Penlight Side", -180f, 180f);
            DrawPhaseValue(_bodyLean, "Body Lean", -45f, 45f);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Drum"))
                {
                    var asset = (FanlightMotionAsset)target;
                    Undo.RecordObject(asset, "Generate Drum Motion");
                    asset.ResetToDrum();
                    EditorUtility.SetDirty(asset);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Bake 64 Samples"))
                {
                    var asset = (FanlightMotionAsset)target;
                    Undo.RecordObject(asset, "Bake Fanlight Motion");
                    asset.Bake();
                    EditorUtility.SetDirty(asset);
                }
            }

            var motionAsset = (FanlightMotionAsset)target;
            EditorGUILayout.HelpBox(
                motionAsset.HasValidBake
                    ? "64 periodic samples are stored inside this asset."
                    : "The baked sample data is invalid.",
                motionAsset.HasValidBake ? MessageType.Info : MessageType.Error);
        }

        private void DrawPhaseValue(SerializedProperty property, string label, float minimum, float maximum)
        {
            var curve = property.animationCurveValue ?? new AnimationCurve();
            var value = curve.length > 0 ? curve.Evaluate(_phase) : 0f;
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.FloatField(label, value);

            if (!EditorGUI.EndChangeCheck()) return;

            value = Mathf.Clamp(value, minimum, maximum);
            var keyIndex = FindKey(curve, _phase);
            if (keyIndex >= 0)
            {
                curve.MoveKey(keyIndex, new Keyframe(_phase, value));
                curve.SmoothTangents(keyIndex, 0f);
            }
            else
            {
                keyIndex = curve.AddKey(_phase, value);
                if (keyIndex >= 0) curve.SmoothTangents(keyIndex, 0f);
            }

            property.animationCurveValue = curve;
        }

        private static int FindKey(AnimationCurve curve, float phase)
        {
            for (var i = 0; i < curve.length; i++)
            {
                if (Mathf.Abs(curve.keys[i].time - phase) <= 0.0001f) return i;
            }

            return -1;
        }
    }
}
