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
            Sasage,
            PowerPump,
            DoublePump,
            DiagonalPump,
            ForwardThrust,
            OverheadSwing,
            Circle,
            FigureEight,
            GrooveBounce,
            RaisedSway
        }

        // Fields

        private static readonly string[] PresetNames =
        {
            "Drum",
            "Wiper",
            "Sasage",
            "Power Pump",
            "Double Pump",
            "Diagonal Pump",
            "Forward Thrust",
            "Overhead Swing",
            "Circle",
            "Figure Eight",
            "Groove Bounce",
            "Raised Sway"
        };

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
        private float _generatorIntensity = 1f;
        private bool _circleClockwise = true;
        private float _phase;

        // Methods

        public override void OnInspectorGUI()
        {
            var asset = (FanlightMotionAsset)target;
            DrawGenerator(asset);
            DrawSampleStatus(asset);
            DrawPhaseInspector(asset);
        }

        private void DrawGenerator(FanlightMotionAsset asset)
        {
            EditorGUILayout.LabelField("Transform Motion Generator", EditorStyles.boldLabel);
            _preset = (MotionPreset)EditorGUILayout.Popup("Motion", (int)_preset, PresetNames);
            if (_preset != MotionPreset.Drum)
            {
                _generatorIntensity = EditorGUILayout.Slider("Intensity", _generatorIntensity, 0.65f, 1.25f);
            }

            switch (_preset)
            {
                case MotionPreset.Wiper:
                    _wiperSweepAngle = EditorGUILayout.Slider("Sweep Angle", _wiperSweepAngle, 20f, 75f);
                    _wiperArmElevation = EditorGUILayout.Slider("Hand Elevation", _wiperArmElevation, 20f, 70f);
                    _wiperArmExtension = EditorGUILayout.Slider("Hand Reach", _wiperArmExtension, 0.5f, 1f);
                    _wiperPenlightElevation = EditorGUILayout.Slider(
                        "Penlight Elevation",
                        _wiperPenlightElevation,
                        30f,
                        90f);
                    break;
                case MotionPreset.Sasage:
                    _sasageLowElevation = EditorGUILayout.Slider("Low Elevation", _sasageLowElevation, -10f, 45f);
                    _sasageHighElevation = EditorGUILayout.Slider("High Elevation", _sasageHighElevation, 30f, 90f);
                    _sasageLowExtension = EditorGUILayout.Slider("Low Reach", _sasageLowExtension, 0.4f, 0.9f);
                    _sasageHighExtension = EditorGUILayout.Slider("High Reach", _sasageHighExtension, 0.7f, 1f);
                    _sasageHoldRatio = EditorGUILayout.Slider("Top Hold Ratio", _sasageHoldRatio, 0.1f, 0.55f);
                    break;
                case MotionPreset.Circle:
                    _circleClockwise = EditorGUILayout.Toggle("Clockwise", _circleClockwise);
                    break;
            }

            EditorGUILayout.HelpBox(
                "The generator writes finished cyclic Body, Hand, and Penlight transforms. Runtime does not evaluate Animation Curves or preset types.",
                MessageType.Info);

            if (!GUILayout.Button("Generate Transform Motion")) return;

            Undo.RecordObject(asset, $"Generate {_preset} Motion");
            Generate(asset);
            EditorUtility.SetDirty(asset);
        }

        private void DrawSampleStatus(FanlightMotionAsset asset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Baked Transform Clip", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Source Samples", asset.SampleCount);
                EditorGUILayout.IntField("Runtime Samples", FanlightMotionAsset.RuntimeSampleCount);
            }

            EditorGUILayout.HelpBox(
                asset.HasValidBake
                    ? "Cyclic transform samples are valid. The final source sample wraps directly to sample zero."
                    : "This asset uses an obsolete or invalid motion format. Generate a transform motion.",
                asset.HasValidBake ? MessageType.Info : MessageType.Error);
        }

        private void DrawPhaseInspector(FanlightMotionAsset asset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pose At Phase", EditorStyles.boldLabel);
            _phase = EditorGUILayout.Slider("Cycle Phase", _phase, 0f, 1f);
            if (!asset.HasValidBake) return;

            var sample = asset.Sample(_phase);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector3Field("Body Position", sample.BodyPosition);
                EditorGUILayout.Vector3Field("Body Rotation", sample.BodyRotation.eulerAngles);
                EditorGUILayout.Vector3Field("Hand Position", sample.HandPosition);
                EditorGUILayout.Vector3Field("Penlight Up", sample.PenlightRotation * Vector3.up);
            }
        }

        private void Generate(FanlightMotionAsset asset)
        {
            switch (_preset)
            {
                case MotionPreset.Drum:
                    FanlightMotionPresetGenerator.GenerateDrum(asset);
                    break;
                case MotionPreset.Wiper:
                    FanlightMotionPresetGenerator.GenerateWiper(
                        asset,
                        _wiperSweepAngle,
                        _wiperArmElevation,
                        _wiperArmExtension,
                        _wiperPenlightElevation,
                        _generatorIntensity);
                    break;
                case MotionPreset.Sasage:
                    FanlightMotionPresetGenerator.GenerateSasage(
                        asset,
                        _sasageLowElevation,
                        _sasageHighElevation,
                        _sasageLowExtension,
                        _sasageHighExtension,
                        _sasageHoldRatio,
                        _generatorIntensity);
                    break;
                case MotionPreset.PowerPump:
                    FanlightMotionPresetGenerator.GeneratePowerPump(asset, _generatorIntensity);
                    break;
                case MotionPreset.DoublePump:
                    FanlightMotionPresetGenerator.GenerateDoublePump(asset, _generatorIntensity);
                    break;
                case MotionPreset.DiagonalPump:
                    FanlightMotionPresetGenerator.GenerateDiagonalPump(asset, _generatorIntensity);
                    break;
                case MotionPreset.ForwardThrust:
                    FanlightMotionPresetGenerator.GenerateForwardThrust(asset, _generatorIntensity);
                    break;
                case MotionPreset.OverheadSwing:
                    FanlightMotionPresetGenerator.GenerateOverheadSwing(asset, _generatorIntensity);
                    break;
                case MotionPreset.Circle:
                    FanlightMotionPresetGenerator.GenerateCircle(asset, _generatorIntensity, _circleClockwise);
                    break;
                case MotionPreset.FigureEight:
                    FanlightMotionPresetGenerator.GenerateFigureEight(asset, _generatorIntensity);
                    break;
                case MotionPreset.GrooveBounce:
                    FanlightMotionPresetGenerator.GenerateGrooveBounce(asset, _generatorIntensity);
                    break;
                case MotionPreset.RaisedSway:
                    FanlightMotionPresetGenerator.GenerateRaisedSway(asset, _generatorIntensity);
                    break;
            }
        }
    }
}
