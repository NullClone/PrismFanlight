using System;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(PrismFanlight))]
    public sealed class PrismFanlightEditor : UnityEditor.Editor
    {
        // Fields

        private readonly PrismFanlightScenePreview _scenePreview = new();

        private SerializedProperty _mesh;
        private SerializedProperty _material;
        private SerializedProperty _computeShader;
        private SerializedProperty _renderingLayerMask;
        private SerializedProperty _enableCulling;
        private SerializedProperty _visibilityUpdate;
        private SerializedProperty _animationUpdate;
        private SerializedProperty _tempo;
        private SerializedProperty _cullingCamera;
        private SerializedProperty _audience;
        private SerializedProperty _motionPreset;
        private SerializedProperty _motion;
        private SerializedProperty _colorPreset;
        private SerializedProperty _color;

        private bool _enablePreview = true;


        // Methods

        private void OnEnable()
        {
            _mesh = serializedObject.FindProperty(nameof(_mesh));
            _material = serializedObject.FindProperty(nameof(_material));
            _computeShader = serializedObject.FindProperty(nameof(_computeShader));
            _renderingLayerMask = serializedObject.FindProperty(nameof(_renderingLayerMask));
            _enableCulling = serializedObject.FindProperty(nameof(_enableCulling));
            _visibilityUpdate = serializedObject.FindProperty(nameof(_visibilityUpdate));
            _animationUpdate = serializedObject.FindProperty(nameof(_animationUpdate));
            _tempo = serializedObject.FindProperty(nameof(_tempo));
            _cullingCamera = serializedObject.FindProperty(nameof(_cullingCamera));
            _audience = serializedObject.FindProperty(nameof(_audience));
            _motionPreset = serializedObject.FindProperty(nameof(_motionPreset));
            _motion = serializedObject.FindProperty(nameof(_motion));
            _colorPreset = serializedObject.FindProperty(nameof(_colorPreset));
            _color = serializedObject.FindProperty(nameof(_color));
        }

        private void OnSceneGUI()
        {
            if (!_enablePreview) return;

            var instance = target as PrismFanlight;

            _scenePreview.Draw(instance);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var instance = target as PrismFanlight;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_computeShader, new GUIContent("Compute Shader"));
            }

            DrawRenderingSection();

            DrawLayoutSection();
            DrawTempoSection();
            DrawMotionSection(instance);
            DrawColorSection(instance);

            serializedObject.ApplyModifiedProperties();

            DrawDebugSection(instance);
        }

        private void DrawRenderingSection()
        {
            if (_computeShader.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign PrismFanlightIndirect.compute to generate instance data on the GPU.", MessageType.Warning);
            }

            PrismFanlightEditorStyles.DrawSection("| Rendering", () =>
            {
                EditorGUILayout.PropertyField(_mesh, new GUIContent("Mesh"));

                if (_mesh.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Stick Mesh is required.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(_material, new GUIContent("Material"));

                if (_material.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Indirect rendering material is required.", MessageType.Warning);
                }

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();

                var mask = EditorGUILayout.RenderingLayerMaskField(new GUIContent("Rendering Layer"), (uint)_renderingLayerMask.longValue);

                if (EditorGUI.EndChangeCheck())
                {
                    _renderingLayerMask.longValue = mask;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Update Mode", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_animationUpdate.FindPropertyRelative("_mode"), new GUIContent("Animation"));

                if (IsFixedRate(_animationUpdate))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_animationUpdate.FindPropertyRelative("_targetFrameRate"), new GUIContent("Frame Rate"));
                    }
                }

                EditorGUILayout.PropertyField(_visibilityUpdate.FindPropertyRelative("_mode"), new GUIContent("Visibility"));

                if (IsFixedRate(_visibilityUpdate))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_visibilityUpdate.FindPropertyRelative("_targetFrameRate"), new GUIContent("Frame Rate"));
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Culling", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_enableCulling, new GUIContent("Enable Culling"));
                EditorGUILayout.PropertyField(_cullingCamera, new GUIContent("Culling Camera"));

                if (_enableCulling.boolValue && _cullingCamera.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Culling is enabled but no camera is assigned. The component will try Camera.main at runtime.", MessageType.Info);
                }
            });
        }

        private void DrawLayoutSection()
        {
            PrismFanlightEditorStyles.DrawSection("| Layout", () =>
            {
                EditorGUILayout.LabelField("Blocks", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_audience.FindPropertyRelative("blockCount"), new GUIContent("Block Count"));
                EditorGUILayout.PropertyField(_audience.FindPropertyRelative("aisleWidth"), new GUIContent("Aisle Width"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Seats", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_audience.FindPropertyRelative("seatPerBlock"), new GUIContent("Seats Per Block"));
                EditorGUILayout.PropertyField(_audience.FindPropertyRelative("seatPitch"), new GUIContent("Seat Pitch"));
            });
        }

        private void DrawTempoSection()
        {
            PrismFanlightEditorStyles.DrawSection("| Tempo", () =>
            {
                EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("enabled"), new GUIContent("Enable"));

                using (new EditorGUI.DisabledScope(!_tempo.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("bpm"), new GUIContent("BPM"));
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("beatsPerBar"));
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("clockSource"));

                    var clockSource = (FanlightTempoClockSource)_tempo.FindPropertyRelative("clockSource").enumValueIndex;

                    if (clockSource == FanlightTempoClockSource.AudioSourceTime)
                    {
                        EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("audioSource"));

                        if (_tempo.FindPropertyRelative("audioSource").objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("Audio Source Time needs an AudioSource with a clip before BPM sync can run.", MessageType.Info);
                        }
                    }
                    else if (clockSource == FanlightTempoClockSource.ManualTime)
                    {
                        EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("manualTime"));
                    }

                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("offsetSeconds"));
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("latencyCompensationSeconds"));

                    if (IsFixedRate(_animationUpdate))
                    {
                        EditorGUILayout.HelpBox("BPM motion is regenerated on the Animation update lane. Use Every Frame for the tightest music sync.", MessageType.Info);
                    }
                }
            });
        }

        private void DrawMotionSection(PrismFanlight fanlight)
        {
            DrawPresetSection(
                "| Motion",
                _motionPreset,
                () => DrawMotionFields(_motion),
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    PrismFanlightPresetUtility.CreateMotionPreset(fanlight, fanlight.GetMotion());
                });
        }

        private void DrawColorSection(PrismFanlight fanlight)
        {
            DrawPresetSection(
                "| Color",
                _colorPreset,
                () => DrawColorFields(_color),
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    PrismFanlightPresetUtility.CreateColorPreset(fanlight, fanlight.GetColorSettings());
                });
        }

        private void DrawDebugSection(PrismFanlight fanlight)
        {
            var diagnostics = fanlight.GetDiagnostics();

            PrismFanlightEditorStyles.DrawSection("| Debug", () =>
            {
                _enablePreview = EditorGUILayout.Toggle("Enable Preview", _enablePreview);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
                PrismFanlightEditorStyles.DrawStat("GPU Ready", diagnostics.IsGpuReady ? "Yes" : "No");
                PrismFanlightEditorStyles.DrawStat("Total Seats", diagnostics.TotalSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Visible Seats", diagnostics.VisibleSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Blocks", diagnostics.BlockCount.ToString("N0"));
            });
        }


        private static void DrawMotionFields(SerializedProperty motion)
        {
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("frequency"), new GUIContent("Frequency"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("randomPhase"), new GUIContent("Random Phase"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("phaseNoiseAmount"), new GUIContent("Phase Noise"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("phaseNoiseSpeed"), new GUIContent("Phase Noise Speed"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("reactionDelay"), new GUIContent("Reaction Delay"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("tempoDrift"), new GUIContent("Tempo Drift"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BPM Sync", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("beatSyncAmount"), new GUIContent("Beat Sync Amount"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("beatsPerSwing"), new GUIContent("Beats Per Swing"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("beatPhaseOffset"), new GUIContent("Beat Phase Offset"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("downbeatAccent"), new GUIContent("Downbeat Accent"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("beatReactionDelay"), new GUIContent("Beat Reaction Delay"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("beatSeatJitter"), new GUIContent("Beat Seat Jitter"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("beatBlockDelay"), new GUIContent("Beat Block Delay"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Swing Shape", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("armLength"), new GUIContent("Arm Length"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("minAngle"), new GUIContent("Min Angle"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("maxAngle"), new GUIContent("Max Angle"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("snapAmount"), new GUIContent("Snap"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("holdAmount"), new GUIContent("Hold"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("flickAmount"), new GUIContent("Flick"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("returnBias"), new GUIContent("Return Bias"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Direction / Axis", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("baseAxis"), new GUIContent("Base Axis"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("forwardBackAmount"), new GUIContent("Forward / Back"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("verticalAmount"), new GUIContent("Vertical"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("axisRandomness"), new GUIContent("Axis Randomness"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("axisNoiseAmount"), new GUIContent("Axis Noise"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("axisNoiseSpeed"), new GUIContent("Axis Noise Speed"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Variation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("seatJitter"), new GUIContent("Seat Jitter"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("heightJitter"), new GUIContent("Height Jitter"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("armLengthJitter"), new GUIContent("Arm Length Jitter"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Humanization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("enthusiasm"), new GUIContent("Enthusiasm"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("enthusiasmVariation"), new GUIContent("Enthusiasm Variation"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("restAmount"), new GUIContent("Rest Amount"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("restIntensity"), new GUIContent("Rest Intensity"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("restCycleDuration"), new GUIContent("Rest Cycle Duration"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("restDuration"), new GUIContent("Rest Duration"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("restFadeDuration"), new GUIContent("Rest Fade Duration"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("restPhaseRandomness"), new GUIContent("Rest Phase Randomness"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("smallMotionRatio"), new GUIContent("Small Motion Ratio"));
        }

        private static void DrawColorFields(SerializedProperty color)
        {
            var mode = color.FindPropertyRelative("mode");
            var colorMode = (FanlightColorMode)mode.enumValueIndex;

            EditorGUILayout.PropertyField(mode);

            switch (colorMode)
            {
                case FanlightColorMode.Single:
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("primaryColor"), new GUIContent("Color"));
                    DrawIntensityField(color);
                    EditorGUILayout.HelpBox("Single uses material properties at draw time, so it does not regenerate the GPU color buffer.", MessageType.Info);
                    break;

                case FanlightColorMode.Random:
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("paletteColors"), new GUIContent("Palette"), true);
                    DrawIntensityField(color);
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
                    EditorGUILayout.HelpBox("Random chooses one fixed palette color per seat. The GPU color buffer updates only when color settings or layout are rebuilt.", MessageType.Info);
                    break;

                case FanlightColorMode.Gradient:
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("primaryColor"), new GUIContent("Start Color"));
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("secondaryColor"), new GUIContent("End Color"));
                    DrawIntensityField(color);
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
                    EditorGUILayout.HelpBox("Gradient keeps the existing block-width gradient behavior.", MessageType.Info);
                    break;
            }
        }

        private static void DrawIntensityField(SerializedProperty color)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brightness", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(color.FindPropertyRelative("intensity"), new GUIContent("Intensity"));
        }

        private static void DrawPresetSection(string title, SerializedProperty preset, Action drawLocalSettings, Action createPreset)
        {
            PrismFanlightEditorStyles.DrawSection(title, () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(preset, new GUIContent("Preset"));
                }

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(preset.objectReferenceValue != null))
                {
                    drawLocalSettings();
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Preset"))
                    {
                        createPreset();
                    }

                    using (new EditorGUI.DisabledScope(preset.objectReferenceValue == null))
                    {
                        if (GUILayout.Button("Select"))
                        {
                            Selection.activeObject = preset.objectReferenceValue;
                        }

                        if (GUILayout.Button("Use Local"))
                        {
                            preset.objectReferenceValue = null;
                        }
                    }
                }
            });
        }

        private static bool IsFixedRate(SerializedProperty property)
        {
            var mode = property.FindPropertyRelative("_mode");
            return mode.enumValueIndex == (int)FanlightGpuUpdateMode.FixedRate;
        }
    }
}
