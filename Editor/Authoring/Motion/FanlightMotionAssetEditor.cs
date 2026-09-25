using System;
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
            Idle = 12,
            Drum = 0,
            Wiper = 1,
            Sasage = 2,
            Cheer = 13
        }


        // Fields

        private bool _drumFoldoutTiming = true;
        private bool _drumFoldoutArm = true;
        private bool _drumFoldoutPitch = true;
        private bool _drumFoldoutBody = true;
        private bool _drumAutoBake;
        private bool _wiperFoldoutTiming = true;
        private bool _wiperFoldoutArm = true;
        private bool _wiperFoldoutPenlight = true;
        private bool _wiperFoldoutBody = true;
        private bool _wiperAutoBake;
        private bool _sasageFoldoutTiming = true;
        private bool _sasageFoldoutArm = true;
        private bool _sasageFoldoutPenlight = true;
        private bool _sasageFoldoutBody = true;
        private bool _sasageAutoBake;
        private float _phase;


        private SettingsContainer _settingsContainer;
        private SerializedObject _settingsEditor;
        private SerializedProperty _settingsProperty;
        private bool _generateRequested;
        private string _generationError;


        // Properties

        private MotionPreset Preset
        {
            get => (MotionPreset)_settingsProperty.FindPropertyRelative("_preset").intValue;
            set => _settingsProperty.FindPropertyRelative("_preset").intValue = (int)value;
        }

        private DrumParameters DrumParams => Settings.DrumParams;

        private WiperParameters WiperParams => Settings.WiperParams;

        private SasageParameters SasageParams => Settings.SasageParams;


        private float GeneratorIntensity
        {
            get => _settingsProperty.FindPropertyRelative("_generatorIntensity").floatValue;
            set => _settingsProperty.FindPropertyRelative("_generatorIntensity").floatValue = value;
        }

        private FanlightMotionGeneratorSettings Settings => _settingsContainer.Settings;

        private bool AutoBake => Preset switch
        {
            MotionPreset.Drum => _drumAutoBake,
            MotionPreset.Wiper => _wiperAutoBake,
            MotionPreset.Sasage => _sasageAutoBake,
            _ => false
        };


        // Methods

        private void OnEnable()
        {
            _settingsContainer = CreateInstance<SettingsContainer>();

            LoadSettings();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;

            _settingsEditor?.Dispose();

            if (_settingsContainer != null) DestroyImmediate(_settingsContainer);
        }

        private void LoadSettings()
        {
            var asset = (FanlightMotionAsset)target;
            var settings = new FanlightMotionGeneratorSettings();

            if (!string.IsNullOrEmpty(asset.EditorGeneratorSettings))
            {
                JsonUtility.FromJsonOverwrite(asset.EditorGeneratorSettings, settings);
            }

            _settingsContainer.Settings = settings;
            _settingsEditor?.Dispose();
            _settingsEditor = new SerializedObject(_settingsContainer);
            _settingsProperty = _settingsEditor.FindProperty("_settings");
            _generationError = null;
        }

        private void OnUndoRedo()
        {
            LoadSettings();
            Repaint();
        }

        private void DrawDrumParameter(string fieldName)
        {
            DrawGeneratorParameter("_drumParams", fieldName);
        }

        private void DrawWiperParameter(string fieldName)
        {
            DrawGeneratorParameter("_wiperParams", fieldName);
        }

        private void DrawSasageParameter(string fieldName)
        {
            DrawGeneratorParameter("_sasageParams", fieldName);
        }

        private void DrawGeneratorParameter(string parameterProperty, string fieldName)
        {
            var field = "_" + char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);

            EditorGUILayout.PropertyField(_settingsProperty.FindPropertyRelative(parameterProperty).FindPropertyRelative(field));
        }


        public override void OnInspectorGUI()
        {
            var asset = (FanlightMotionAsset)target;
            DrawGenerator(asset);
            DrawSampleStatus(asset);
            DrawPhaseInspector(asset);
        }

        private void DrawGenerator(FanlightMotionAsset asset)
        {
            _settingsEditor.Update();
            var before = JsonUtility.ToJson(Settings);
            var previousPreset = Preset;
            _generateRequested = false;
            EditorGUILayout.LabelField("Motion Generator", EditorStyles.boldLabel);
            Preset = (MotionPreset)EditorGUILayout.EnumPopup("Preset", Preset);

            GeneratorIntensity = EditorGUILayout.Slider("Intensity", GeneratorIntensity, 0.65f, 1.25f);

            switch (Preset)
            {
                case MotionPreset.Drum:
                    DrawDrumControls();
                    break;
                case MotionPreset.Wiper:
                    DrawWiperControls();
                    break;
                case MotionPreset.Sasage:
                    DrawSasageControls();
                    break;
            }

            _settingsEditor.ApplyModifiedPropertiesWithoutUndo();
            var after = JsonUtility.ToJson(Settings);
            var changed = before != after;
            var generate = GUILayout.Button("Generate Motion") || _generateRequested
                                                               || (changed && previousPreset == Preset && AutoBake);
            if (changed || generate)
            {
                Undo.RecordObject(asset, generate ? "Generate Motion" : "Edit Motion Settings");
                try
                {
                    if (generate)
                    {
                        Generate(asset);
                        Settings.BakedPreset = (int)Preset;
                    }

                    asset.EditorGeneratorSettings = JsonUtility.ToJson(Settings);
                    EditorUtility.SetDirty(asset);
                    _generationError = null;
                }
                catch (ArgumentException exception)
                {
                    asset.EditorGeneratorSettings = after;
                    EditorUtility.SetDirty(asset);
                    _generationError = exception.Message;
                }
            }

            if (!string.IsNullOrEmpty(_generationError))
                EditorGUILayout.HelpBox(_generationError, MessageType.Error);
            if (!AutoBake)
                EditorGUILayout.HelpBox("Generate Motion applies the current settings to the motion asset.", MessageType.Info);
        }

        private void DrawDrumControls()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                _drumAutoBake = EditorGUILayout.ToggleLeft("Auto Bake on Change", _drumAutoBake, GUILayout.Width(160));
                if (GUILayout.Button("Reset Defaults", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Settings.DrumParams = DrumParameters.CreateDefault();
                    Settings.GeneratorIntensity = 1f;
                    _settingsEditor.Update();
                    _generateRequested = true;
                    GUI.FocusControl(null);
                }
            }

            _drumFoldoutTiming = EditorGUILayout.Foldout(_drumFoldoutTiming, "Timing & Rhythm", true, EditorStyles.foldoutHeader);
            if (_drumFoldoutTiming)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawDrumParameter(nameof(DrumParameters.RecoveryDuration));
                    DrawDrumParameter(nameof(DrumParameters.BodyPhaseLag));
                    DrawDrumParameter(nameof(DrumParameters.WristPhaseLag));
                }
            }

            _drumFoldoutArm = EditorGUILayout.Foldout(_drumFoldoutArm, "Arm Swing & Reach", true, EditorStyles.foldoutHeader);
            if (_drumFoldoutArm)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawDrumParameter(nameof(DrumParameters.BaseElevation));
                    DrawDrumParameter(nameof(DrumParameters.ElevationAmplitude));
                    DrawDrumParameter(nameof(DrumParameters.BaseExtension));
                    DrawDrumParameter(nameof(DrumParameters.LiftExtension));
                    DrawDrumParameter(nameof(DrumParameters.RecoveryExtensionArc));
                    DrawDrumParameter(nameof(DrumParameters.StrikeExtensionArc));
                    DrawDrumParameter(nameof(DrumParameters.BaseSideAngle));
                    DrawDrumParameter(nameof(DrumParameters.RecoverySideArc));
                    DrawDrumParameter(nameof(DrumParameters.StrikeSideArc));
                }
            }

            _drumFoldoutPitch = EditorGUILayout.Foldout(_drumFoldoutPitch, "Penlight Snap & Pitch", true, EditorStyles.foldoutHeader);
            if (_drumFoldoutPitch)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawDrumParameter(nameof(DrumParameters.BasePitch));
                    DrawDrumParameter(nameof(DrumParameters.PitchAmplitude));
                    DrawDrumParameter(nameof(DrumParameters.RecoveryPitchArc));
                    DrawDrumParameter(nameof(DrumParameters.StrikePitchArc));
                    DrawDrumParameter(nameof(DrumParameters.PitchPivot));
                }
            }

            _drumFoldoutBody = EditorGUILayout.Foldout(_drumFoldoutBody, "Body Bounce & Lean", true, EditorStyles.foldoutHeader);
            if (_drumFoldoutBody)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawDrumParameter(nameof(DrumParameters.BodySink));
                    DrawDrumParameter(nameof(DrumParameters.BodyPush));
                    DrawDrumParameter(nameof(DrumParameters.BaseBodyLean));
                    DrawDrumParameter(nameof(DrumParameters.BodyLeanAmplitude));
                }
            }
        }

        private void DrawWiperControls()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                _wiperAutoBake = EditorGUILayout.ToggleLeft("Auto Bake on Change", _wiperAutoBake, GUILayout.Width(160));
                if (GUILayout.Button("Reset Defaults", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Settings.WiperParams = WiperParameters.CreateDefault();
                    Settings.GeneratorIntensity = 1f;
                    _settingsEditor.Update();
                    _generateRequested = true;
                    GUI.FocusControl(null);
                }
            }

            _wiperFoldoutTiming = EditorGUILayout.Foldout(_wiperFoldoutTiming, "Timing & Rhythm", true, EditorStyles.foldoutHeader);
            if (_wiperFoldoutTiming)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawWiperParameter(nameof(WiperParameters.TurnaroundEase));
                    DrawWiperParameter(nameof(WiperParameters.BodyPhaseLag));
                    DrawWiperParameter(nameof(WiperParameters.WristPhaseLag));
                }
            }

            _wiperFoldoutArm = EditorGUILayout.Foldout(_wiperFoldoutArm, "Arm Swing & Reach", true, EditorStyles.foldoutHeader);
            if (_wiperFoldoutArm)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawWiperParameter(nameof(WiperParameters.SideBias));
                    DrawWiperParameter(nameof(WiperParameters.BaseElevation));
                    DrawWiperParameter(nameof(WiperParameters.SweepAngle));
                    DrawWiperParameter(nameof(WiperParameters.CenterElevationArc));
                    DrawWiperParameter(nameof(WiperParameters.BaseExtension));
                    DrawWiperParameter(nameof(WiperParameters.CenterExtensionArc));
                }
            }

            _wiperFoldoutPenlight = EditorGUILayout.Foldout(_wiperFoldoutPenlight, "Penlight Follow", true, EditorStyles.foldoutHeader);
            if (_wiperFoldoutPenlight)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawWiperParameter(nameof(WiperParameters.PenlightElevation));
                    DrawWiperParameter(nameof(WiperParameters.PenlightSideAmplitude));
                    DrawWiperParameter(nameof(WiperParameters.PenlightCenterElevationArc));
                }
            }

            _wiperFoldoutBody = EditorGUILayout.Foldout(_wiperFoldoutBody, "Body Sway & Balance", true, EditorStyles.foldoutHeader);
            if (_wiperFoldoutBody)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawWiperParameter(nameof(WiperParameters.BodySideShift));
                    DrawWiperParameter(nameof(WiperParameters.BodyVerticalBounce));
                    DrawWiperParameter(nameof(WiperParameters.BaseBodyLean));
                    DrawWiperParameter(nameof(WiperParameters.BodyYawAmplitude));
                    DrawWiperParameter(nameof(WiperParameters.BodyRollAmplitude));
                }
            }
        }

        private void DrawSasageControls()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                _sasageAutoBake = EditorGUILayout.ToggleLeft("Auto Bake on Change", _sasageAutoBake, GUILayout.Width(160));
                if (GUILayout.Button("Reset Defaults", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Settings.SasageParams = SasageParameters.CreateDefault();
                    Settings.GeneratorIntensity = 1f;
                    _settingsEditor.Update();
                    _generateRequested = true;
                    GUI.FocusControl(null);
                }
            }

            _sasageFoldoutTiming = EditorGUILayout.Foldout(_sasageFoldoutTiming, "Timing & Rhythm", true, EditorStyles.foldoutHeader);
            if (_sasageFoldoutTiming)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSasageParameter(nameof(SasageParameters.WristPhaseLag));
                }
            }

            _sasageFoldoutArm = EditorGUILayout.Foldout(_sasageFoldoutArm, "Arm Trajectory", true, EditorStyles.foldoutHeader);
            if (_sasageFoldoutArm)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSasageParameter(nameof(SasageParameters.SideAmplitude));
                    DrawSasageParameter(nameof(SasageParameters.BaseHandHeight));
                    DrawSasageParameter(nameof(SasageParameters.VerticalAmplitude));
                    DrawSasageParameter(nameof(SasageParameters.BaseHandDepth));
                    DrawSasageParameter(nameof(SasageParameters.DepthAmplitude));
                }
            }

            _sasageFoldoutPenlight = EditorGUILayout.Foldout(_sasageFoldoutPenlight, "Penlight Follow", true, EditorStyles.foldoutHeader);
            if (_sasageFoldoutPenlight)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSasageParameter(nameof(SasageParameters.PenlightTangentWeight));
                    DrawSasageParameter(nameof(SasageParameters.PenlightUpwardBias));
                }
            }

            _sasageFoldoutBody = EditorGUILayout.Foldout(_sasageFoldoutBody, "Body Bounce & Lean", true, EditorStyles.foldoutHeader);
            if (_sasageFoldoutBody)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSasageParameter(nameof(SasageParameters.BodyRiseLift));
                    DrawSasageParameter(nameof(SasageParameters.BaseBodyLean));
                    DrawSasageParameter(nameof(SasageParameters.BodyLeanAmplitude));
                    DrawSasageParameter(nameof(SasageParameters.BodyRollAmplitude));
                }
            }
        }

        private static void DrawSampleStatus(FanlightMotionAsset asset)
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
            switch (Preset)
            {
                case MotionPreset.Idle:
                    FanlightMotionPresetGenerator.GenerateIdle(asset, GeneratorIntensity);
                    break;
                case MotionPreset.Drum:
                    FanlightMotionPresetGenerator.GenerateDrum(asset, DrumParams, GeneratorIntensity);
                    break;
                case MotionPreset.Wiper:
                    FanlightMotionPresetGenerator.GenerateWiper(asset, WiperParams, GeneratorIntensity);
                    break;
                case MotionPreset.Sasage:
                    FanlightMotionPresetGenerator.GenerateSasage(asset, SasageParams, GeneratorIntensity);
                    break;
                case MotionPreset.Cheer:
                    FanlightMotionPresetGenerator.GenerateCheer(asset, GeneratorIntensity);
                    break;
            }
        }


        private sealed class SettingsContainer : ScriptableObject
        {
            // Fields

            [SerializeField]
            private FanlightMotionGeneratorSettings _settings = new();

            // Properties

            internal FanlightMotionGeneratorSettings Settings
            {
                get => _settings;
                set => _settings = value;
            }
        }
    }
}
