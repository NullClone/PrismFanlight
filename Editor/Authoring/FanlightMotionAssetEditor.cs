using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightMotionAsset))]
    internal sealed class FanlightMotionAssetEditor : UnityEditor.Editor
    {
        private enum MotionPreset
        {
            Drum,
            Wiper,
            Sasage
        }


        // Fields

        private static readonly string[] PresetNames = { "Drum", "Wiper", "Sasage" };

        private SerializedProperty _referenceArmElevation;
        private SerializedProperty _referenceArmSide;
        private SerializedProperty _referenceArmExtension;
        private SerializedProperty _referencePenlightElevation;
        private SerializedProperty _referencePenlightSide;
        private SerializedProperty _referenceBodyLean;
        private SerializedProperty _armElevationAmplitude;
        private SerializedProperty _armSideAmplitude;
        private SerializedProperty _armExtensionAmplitude;
        private SerializedProperty _penlightElevationAmplitude;
        private SerializedProperty _penlightSideAmplitude;
        private SerializedProperty _bodyLeanAmplitude;
        private SerializedProperty _armElevation;
        private SerializedProperty _armSide;
        private SerializedProperty _armExtension;
        private SerializedProperty _penlightElevation;
        private SerializedProperty _penlightSide;
        private SerializedProperty _bodyLean;

        private MotionPreset _preset;
        private float _wiperSweepAngle = 55f;
        private float _wiperArmElevation = 42f;
        private float _wiperArmExtension = 0.88f;
        private float _wiperPenlightElevation = 65f;
        private float _sasageLowElevation = 18f;
        private float _sasageHighElevation = 68f;
        private float _sasageLowExtension = 0.7f;
        private float _sasageHighExtension = 0.96f;
        private float _sasageHoldRatio = 0.4f;
        private float _phase;
        private bool _showReferencePose = true;
        private bool _showAmplitude = true;
        private bool _showCurves = true;
        private bool _showPhaseInspector;


        // Methods

        private void OnEnable()
        {
            _referenceArmElevation = serializedObject.FindProperty(nameof(_referenceArmElevation));
            _referenceArmSide = serializedObject.FindProperty(nameof(_referenceArmSide));
            _referenceArmExtension = serializedObject.FindProperty(nameof(_referenceArmExtension));
            _referencePenlightElevation = serializedObject.FindProperty(nameof(_referencePenlightElevation));
            _referencePenlightSide = serializedObject.FindProperty(nameof(_referencePenlightSide));
            _referenceBodyLean = serializedObject.FindProperty(nameof(_referenceBodyLean));
            _armElevationAmplitude = serializedObject.FindProperty(nameof(_armElevationAmplitude));
            _armSideAmplitude = serializedObject.FindProperty(nameof(_armSideAmplitude));
            _armExtensionAmplitude = serializedObject.FindProperty(nameof(_armExtensionAmplitude));
            _penlightElevationAmplitude = serializedObject.FindProperty(nameof(_penlightElevationAmplitude));
            _penlightSideAmplitude = serializedObject.FindProperty(nameof(_penlightSideAmplitude));
            _bodyLeanAmplitude = serializedObject.FindProperty(nameof(_bodyLeanAmplitude));
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

            var motionAsset = (FanlightMotionAsset)target;

            DrawPresetGenerator();
            DrawReferencePose();
            DrawAmplitude();
            DrawNormalizedCurves();
            DrawPhaseInspector();
            DrawBakeControls(motionAsset);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetGenerator()
        {
            // TODO: プリセットのデフォルトの値を調整する

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);

            _preset = (MotionPreset)GUILayout.Toolbar((int)_preset, PresetNames);

            switch (_preset)
            {
                case MotionPreset.Wiper:
                    _wiperSweepAngle = EditorGUILayout.Slider("Sweep Angle", _wiperSweepAngle, 20f, 75f);
                    _wiperArmElevation = EditorGUILayout.Slider("Arm Elevation", _wiperArmElevation, 20f, 70f);
                    _wiperArmExtension = EditorGUILayout.Slider("Arm Extension", _wiperArmExtension, 0.5f, 1f);
                    _wiperPenlightElevation = EditorGUILayout.Slider(
                        "Penlight Elevation",
                        _wiperPenlightElevation,
                        30f,
                        90f);
                    break;
                case MotionPreset.Sasage:
                    _sasageLowElevation = EditorGUILayout.Slider(
                        "Low Arm Elevation",
                        _sasageLowElevation,
                        -10f,
                        45f);
                    _sasageHighElevation = EditorGUILayout.Slider(
                        "High Arm Elevation",
                        _sasageHighElevation,
                        30f,
                        90f);
                    _sasageLowExtension = EditorGUILayout.Slider(
                        "Low Arm Extension",
                        _sasageLowExtension,
                        0.4f,
                        0.9f);
                    _sasageHighExtension = EditorGUILayout.Slider(
                        "High Arm Extension",
                        _sasageHighExtension,
                        0.7f,
                        1f);
                    _sasageHoldRatio = EditorGUILayout.Slider(
                        "Top Hold Ratio",
                        _sasageHoldRatio,
                        0.1f,
                        0.55f);
                    EditorGUILayout.HelpBox(
                        "Recommended: Beats Per Cycle 4 to 8, Wrist Delay Ratio 0 to 0.03.",
                        MessageType.Info);
                    break;
            }

            if (GUILayout.Button("Generate"))
            {
                GeneratePreset();
            }
        }

        private void DrawReferencePose()
        {
            EditorGUILayout.Space();

            _showReferencePose = EditorGUILayout.Foldout(_showReferencePose, "Reference Pose", true);

            if (!_showReferencePose) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_referenceArmElevation, new GUIContent("Arm Elevation"));
                EditorGUILayout.PropertyField(_referenceArmSide, new GUIContent("Arm Side"));
                EditorGUILayout.PropertyField(_referenceArmExtension, new GUIContent("Arm Extension"));
                EditorGUILayout.PropertyField(_referencePenlightElevation, new GUIContent("Penlight Elevation"));
                EditorGUILayout.PropertyField(_referencePenlightSide, new GUIContent("Penlight Side"));
                EditorGUILayout.PropertyField(_referenceBodyLean, new GUIContent("Body Lean"));
            }
        }

        private void DrawAmplitude()
        {
            EditorGUILayout.Space();

            _showAmplitude = EditorGUILayout.Foldout(_showAmplitude, "Channel Amplitude", true);

            if (!_showAmplitude) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_armElevationAmplitude, new GUIContent("Arm Elevation"));
                EditorGUILayout.PropertyField(_armSideAmplitude, new GUIContent("Arm Side"));
                EditorGUILayout.PropertyField(_armExtensionAmplitude, new GUIContent("Arm Extension"));
                EditorGUILayout.PropertyField(_penlightElevationAmplitude, new GUIContent("Penlight Elevation"));
                EditorGUILayout.PropertyField(_penlightSideAmplitude, new GUIContent("Penlight Side"));
                EditorGUILayout.PropertyField(_bodyLeanAmplitude, new GUIContent("Body Lean"));
            }
        }

        private void DrawNormalizedCurves()
        {
            EditorGUILayout.Space();
            _showCurves = EditorGUILayout.Foldout(_showCurves, "Animation Curves", true);
            if (!_showCurves) return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawNormalizedCurve(_armElevation, "Arm Elevation");
                DrawNormalizedCurve(_armSide, "Arm Side");
                DrawNormalizedCurve(_armExtension, "Arm Extension");
                DrawNormalizedCurve(_penlightElevation, "Penlight Elevation");
                DrawNormalizedCurve(_penlightSide, "Penlight Side");
                DrawNormalizedCurve(_bodyLean, "Body Lean");
            }
        }

        private void DrawPhaseInspector()
        {
            EditorGUILayout.Space();
            _showPhaseInspector = EditorGUILayout.Foldout(_showPhaseInspector, "Pose At Phase", true);
            if (!_showPhaseInspector) return;

            using (new EditorGUI.IndentLevelScope())
            {
                _phase = EditorGUILayout.Slider("Cycle Phase", _phase, 0f, 1f);
                DrawPhaseValue(
                    _armElevation,
                    _referenceArmElevation,
                    _armElevationAmplitude,
                    "Arm Elevation",
                    -90f,
                    90f,
                    " deg");
                DrawPhaseValue(
                    _armSide,
                    _referenceArmSide,
                    _armSideAmplitude,
                    "Arm Side",
                    -90f,
                    90f,
                    " deg");
                DrawPhaseValue(
                    _armExtension,
                    _referenceArmExtension,
                    _armExtensionAmplitude,
                    "Arm Extension",
                    0f,
                    1f,
                    string.Empty);
                DrawPhaseValue(
                    _penlightElevation,
                    _referencePenlightElevation,
                    _penlightElevationAmplitude,
                    "Penlight Elevation",
                    -90f,
                    90f,
                    " deg");
                DrawPhaseValue(
                    _penlightSide,
                    _referencePenlightSide,
                    _penlightSideAmplitude,
                    "Penlight Side",
                    -180f,
                    180f,
                    " deg");
                DrawPhaseValue(
                    _bodyLean,
                    _referenceBodyLean,
                    _bodyLeanAmplitude,
                    "Body Lean",
                    -45f,
                    45f,
                    " deg");

                if (GUILayout.Button("Set Reference From Phase")) SetReferenceFromPhase();
            }
        }

        private void DrawBakeControls(FanlightMotionAsset motionAsset)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Bake 64 Samples"))
            {
                Undo.RecordObject(motionAsset, "Bake Fanlight Motion");
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                motionAsset.Bake();
                EditorUtility.SetDirty(motionAsset);
                serializedObject.Update();
            }

            EditorGUILayout.HelpBox(
                motionAsset.HasValidBake
                    ? "64 periodic samples are stored inside this asset."
                    : "The baked sample data is invalid.",
                motionAsset.HasValidBake ? MessageType.Info : MessageType.Error);
        }

        private void GeneratePreset()
        {
            serializedObject.ApplyModifiedProperties();
            var asset = (FanlightMotionAsset)target;
            Undo.RecordObject(asset, $"Generate {_preset} Motion");
            serializedObject.Update();

            switch (_preset)
            {
                case MotionPreset.Drum:
                    asset.ResetToDrum();
                    break;
                case MotionPreset.Wiper:
                    FanlightMotionPresetGenerator.GenerateWiper(
                        serializedObject,
                        _wiperSweepAngle,
                        _wiperArmElevation,
                        _wiperArmExtension,
                        _wiperPenlightElevation);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    asset.Bake();
                    break;
                case MotionPreset.Sasage:
                    FanlightMotionPresetGenerator.GenerateSasage(
                        serializedObject,
                        _sasageLowElevation,
                        _sasageHighElevation,
                        _sasageLowExtension,
                        _sasageHighExtension,
                        _sasageHoldRatio);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    asset.Bake();
                    break;
            }

            EditorUtility.SetDirty(asset);
            serializedObject.Update();
        }

        private static void DrawNormalizedCurve(SerializedProperty property, string label)
        {
            var curve = property.animationCurveValue ?? AnimationCurve.Linear(0f, 0f, 1f, 0f);
            EditorGUI.BeginChangeCheck();
            curve = EditorGUILayout.CurveField(
                new GUIContent(label),
                curve,
                Color.cyan,
                new Rect(0f, -1f, 1f, 2f));

            if (!EditorGUI.EndChangeCheck()) return;

            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                key.time = Mathf.Clamp01(key.time);
                key.value = Mathf.Clamp(key.value, -1f, 1f);
                keys[i] = key;
            }

            curve.keys = keys;
            property.animationCurveValue = curve;
        }

        private void DrawPhaseValue(
            SerializedProperty curveProperty,
            SerializedProperty referenceProperty,
            SerializedProperty amplitudeProperty,
            string label,
            float minimum,
            float maximum,
            string suffix)
        {
            var curve = curveProperty.animationCurveValue ?? AnimationCurve.Linear(0f, 0f, 1f, 0f);
            var normalized = curve.length > 0 ? Mathf.Clamp(curve.Evaluate(_phase), -1f, 1f) : 0f;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                normalized = EditorGUILayout.FloatField(label, normalized);

                if (EditorGUI.EndChangeCheck())
                {
                    normalized = Mathf.Clamp(normalized, -1f, 1f);
                    var keyIndex = FindKey(curve, _phase);
                    if (keyIndex >= 0)
                    {
                        curve.MoveKey(keyIndex, new Keyframe(_phase, normalized));
                        curve.SmoothTangents(keyIndex, 0f);
                    }
                    else
                    {
                        keyIndex = curve.AddKey(_phase, normalized);
                        if (keyIndex >= 0) curve.SmoothTangents(keyIndex, 0f);
                    }

                    curveProperty.animationCurveValue = curve;
                }

                var output = Mathf.Clamp(
                    referenceProperty.floatValue + normalized * Mathf.Max(0f, amplitudeProperty.floatValue),
                    minimum,
                    maximum);
                GUILayout.Label($"= {output:0.###}{suffix}", GUILayout.Width(88f));
            }
        }

        private static int FindKey(AnimationCurve curve, float phase)
        {
            for (var i = 0; i < curve.length; i++)
            {
                if (Mathf.Abs(curve.keys[i].time - phase) <= 0.0001f) return i;
            }

            return -1;
        }

        private void SetReferenceFromPhase()
        {
            var motionAsset = (FanlightMotionAsset)target;
            Undo.RecordObject(motionAsset, "Set Motion Reference From Phase");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            motionAsset.SetReferenceFromPhase(_phase);
            EditorUtility.SetDirty(motionAsset);
            serializedObject.Update();
        }
    }
}
