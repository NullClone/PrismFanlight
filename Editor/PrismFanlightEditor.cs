using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(PrismFanlight))]
    public sealed class PrismFanlightEditor : UnityEditor.Editor
    {
        // Fields

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
        private SerializedProperty _swingTarget;
        private SerializedProperty _colorPreset;
        private SerializedProperty _color;

        private bool _noiseDetailFoldout;
        private bool _restTimingFoldout;
        private bool _beatSpreadFoldout;

        private bool _enablePreview = true;


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
            _swingTarget = serializedObject.FindProperty(nameof(_swingTarget));
            _colorPreset = serializedObject.FindProperty(nameof(_colorPreset));
            _color = serializedObject.FindProperty(nameof(_color));
        }

        private void OnSceneGUI()
        {
            if (!_enablePreview) return;

            if (target is PrismFanlight fanlight)
            {
                new PrismFanlightScenePreview().Draw(fanlight);
            }
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
            DrawMotionSection(instance);
            DrawTempoSection();
            DrawColorSection(instance);
            DrawDebugSection(instance);

            serializedObject.ApplyModifiedProperties();
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
                if (EditorGUI.EndChangeCheck()) _renderingLayerMask.longValue = mask;

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

                if (_enableCulling.boolValue)
                {
                    EditorGUILayout.PropertyField(_cullingCamera, new GUIContent("Culling Camera"));
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
                var enabledProp = _tempo.FindPropertyRelative("enabled");
                EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enable"));
                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
                {
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("bpm"), new GUIContent("BPM"));
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("beatsPerBar"), new GUIContent("Beats Per Bar"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("clockSource"), new GUIContent("Clock Source"));

                    var clockSource = (FanlightTempoClockSource)_tempo.FindPropertyRelative("clockSource").enumValueIndex;

                    if (clockSource == FanlightTempoClockSource.AudioSourceTime)
                    {
                        var audioSourceProp = _tempo.FindPropertyRelative("audioSource");
                        EditorGUILayout.PropertyField(audioSourceProp, new GUIContent("Audio Source"));
                        if (audioSourceProp.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("Assign an AudioSource with a clip for BPM sync.", MessageType.Info);
                        }
                    }
                    else if (clockSource == FanlightTempoClockSource.ManualTime)
                    {
                        EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("manualTime"), new GUIContent("Manual Time"));
                    }

                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("offsetSeconds"), new GUIContent("Offset"));
                    EditorGUILayout.PropertyField(_tempo.FindPropertyRelative("latencyCompensationSeconds"), new GUIContent("Latency Compensation"));
                    EditorGUILayout.Space();
                    PrismFanlightEditorStyles.DrawSubGroupLabel("Beat Sync");

                    var beatSync = _motion.FindPropertyRelative("beatSync");
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("beatSyncBlend"), new GUIContent("Sync Blend"));
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("beatsPerSwing"), new GUIContent("Beats Per Swing"));
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("beatPhaseOffset"), new GUIContent("Phase Offset"));
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("downbeatAccent"), new GUIContent("Downbeat Accent"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("beatReactionDelay"), new GUIContent("Reaction Delay"));
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("beatSeatJitter"), new GUIContent("Seat Jitter"));
                    EditorGUILayout.PropertyField(beatSync.FindPropertyRelative("beatBlockDelay"), new GUIContent("Block Delay"));
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

        private void DrawMotionFields(SerializedProperty motion)
        {
            var swingProp = motion.FindPropertyRelative("swing");
            var directionProp = motion.FindPropertyRelative("direction");
            var noiseProp = motion.FindPropertyRelative("noise");
            var humanProp = motion.FindPropertyRelative("human");

            PrismFanlightEditorStyles.DrawSubGroupLabel("Swing");

            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("swingSpeed"), new GUIContent("Swing Speed"));
            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("armLength"), new GUIContent("Arm Length"));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Angle Range");

                var minAngleProp = swingProp.FindPropertyRelative("minAngle");
                var maxAngleProp = swingProp.FindPropertyRelative("maxAngle");
                var minAngle = minAngleProp.floatValue;
                var maxAngle = maxAngleProp.floatValue;

                minAngle = EditorGUILayout.FloatField(minAngle);
                EditorGUILayout.MinMaxSlider(ref minAngle, ref maxAngle, 0f, 2f);
                maxAngle = EditorGUILayout.FloatField(maxAngle);

                minAngle = math.round(minAngle * 100f) / 100f;
                maxAngle = math.round(maxAngle * 100f) / 100f;

                minAngleProp.floatValue = minAngle;
                maxAngleProp.floatValue = maxAngle;
            }

            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("snapAmount"), new GUIContent("Snap"));
            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("holdAmount"), new GUIContent("Hold"));
            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("flickAmount"), new GUIContent("Flick"));
            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("returnBias"), new GUIContent("Return Bias"));
            EditorGUILayout.PropertyField(swingProp.FindPropertyRelative("randomPhase"), new GUIContent("Phase Randomness"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Direction");

            var swingModeProp = directionProp.FindPropertyRelative("swingMode");
            EditorGUILayout.PropertyField(swingModeProp, new GUIContent("Swing Mode"));
            var swingMode = (FanlightSwingMode)swingModeProp.enumValueIndex;

            if (swingMode == FanlightSwingMode.WorldDirection)
            {
                EditorGUILayout.PropertyField(directionProp.FindPropertyRelative("swingYaw"), new GUIContent("Direction"));
            }
            else
            {
                EditorGUILayout.PropertyField(_swingTarget, new GUIContent("Swing Target"));
                var aimStrengthProp = directionProp.FindPropertyRelative("aimStrength");
                EditorGUILayout.PropertyField(aimStrengthProp, new GUIContent("Direction Strength"));

                if (aimStrengthProp.floatValue < 1f)
                {
                    EditorGUILayout.PropertyField(directionProp.FindPropertyRelative("swingYaw"), new GUIContent("Fallback Direction"));
                }
            }

            EditorGUILayout.PropertyField(directionProp.FindPropertyRelative("directionSpread"), new GUIContent("Direction Spread"));
            EditorGUILayout.Space();

            PrismFanlightEditorStyles.DrawSubGroupLabel("Noise");

            EditorGUILayout.PropertyField(noiseProp.FindPropertyRelative("phaseIrregularity"), new GUIContent("Phase Irregularity"));
            EditorGUILayout.PropertyField(noiseProp.FindPropertyRelative("phaseIrregularitySpeed"), new GUIContent("Irregularity Speed"));
            EditorGUILayout.PropertyField(noiseProp.FindPropertyRelative("axisNoiseAmount"), new GUIContent("Axis Drift"));
            EditorGUILayout.PropertyField(noiseProp.FindPropertyRelative("axisNoiseSpeed"), new GUIContent("Drift Speed"));
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(noiseProp.FindPropertyRelative("noiseOctaves"), new GUIContent("Octaves"));
                EditorGUILayout.PropertyField(noiseProp.FindPropertyRelative("noiseDetail"), new GUIContent("Detail"));
            }

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Feel");

            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("enthusiasm"), new GUIContent("Enthusiasm"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("enthusiasmVariation"), new GUIContent("Variation"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("lazyFanRatio"), new GUIContent("Lazy Fan Ratio"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("seatJitter"), new GUIContent("Seat Jitter"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("heightJitter"), new GUIContent("Height Jitter"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("armLengthJitter"), new GUIContent("Arm Length Jitter"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("reactionDelay"), new GUIContent("Reaction Delay"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("speedVariation"), new GUIContent("Speed Variation"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Rest");

            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("restProbability"), new GUIContent("Probability"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("restMotionLevel"), new GUIContent("Motion Level"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("restCycleDuration"),
                new GUIContent("Cycle Duration", "How often the rest cycle repeats (seconds)"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("restDuration"),
                new GUIContent("Rest Duration", "How long each rest lasts within the cycle (seconds)"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("restFadeDuration"),
                new GUIContent("Fade Duration", "Time to fade in and out of the rest state (seconds)"));
            EditorGUILayout.PropertyField(humanProp.FindPropertyRelative("restPhaseRandomness"),
                new GUIContent("Phase Randomness", "Per-fan variation in rest cycle timing"));
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
                    break;

                case FanlightColorMode.Random:
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("paletteColors"), new GUIContent("Palette"), true);
                    DrawIntensityField(color);
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
                    break;

                case FanlightColorMode.Gradient:
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("primaryColor"), new GUIContent("Start Color"));
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("secondaryColor"), new GUIContent("End Color"));
                    DrawIntensityField(color);
                    EditorGUILayout.PropertyField(color.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
                    break;
            }
        }

        private static void DrawIntensityField(SerializedProperty color)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brightness", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(color.FindPropertyRelative("intensity"), new GUIContent("Intensity"));
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

        private static void DrawPresetSection(string title, SerializedProperty preset, Action drawLocalSettings, Action createPreset)
        {
            PrismFanlightEditorStyles.DrawSection(title, () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                    EditorGUILayout.PropertyField(preset, new GUIContent("Preset"));

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(preset.objectReferenceValue != null))
                    drawLocalSettings();

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Preset"))
                        createPreset();

                    using (new EditorGUI.DisabledScope(preset.objectReferenceValue == null))
                    {
                        if (GUILayout.Button("Select"))
                            Selection.activeObject = preset.objectReferenceValue;

                        if (GUILayout.Button("Use Local"))
                            preset.objectReferenceValue = null;
                    }
                }
            });
        }

        private static bool IsFixedRate(SerializedProperty property) =>
            property.FindPropertyRelative("_mode").enumValueIndex == (int)FanlightGpuUpdateMode.FixedRate;
    }
}
