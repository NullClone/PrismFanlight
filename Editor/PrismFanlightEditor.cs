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
        private SerializedProperty _enableCulling;
        private SerializedProperty _visibilityUpdate;
        private SerializedProperty _animationUpdate;
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
            _enableCulling = serializedObject.FindProperty(nameof(_enableCulling));
            _visibilityUpdate = serializedObject.FindProperty(nameof(_visibilityUpdate));
            _animationUpdate = serializedObject.FindProperty(nameof(_animationUpdate));
            _cullingCamera = serializedObject.FindProperty(nameof(_cullingCamera));
            _audience = serializedObject.FindProperty(nameof(_audience));
            _motionPreset = serializedObject.FindProperty(nameof(_motionPreset));
            _motion = serializedObject.FindProperty(nameof(_motion));
            _colorPreset = serializedObject.FindProperty(nameof(_colorPreset));
            _color = serializedObject.FindProperty(nameof(_color));
        }

        private void OnSceneGUI()
        {
            if (!_enablePreview || Application.isPlaying) return;

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
                EditorGUILayout.PropertyField(_material, new GUIContent("Material"));

                if (_mesh.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Stick Mesh is required.", MessageType.Warning);
                }

                if (_material.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Indirect rendering material is required.", MessageType.Warning);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Update Mode", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_animationUpdate.FindPropertyRelative("_mode"), new GUIContent("Animation / Color"));

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

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Seats", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_audience.FindPropertyRelative("seatPerBlock"), new GUIContent("Seats Per Block"));
                EditorGUILayout.PropertyField(_audience.FindPropertyRelative("seatPitch"), new GUIContent("Seat Pitch"));
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
            var audience = fanlight.GetAudience();
            var motion = fanlight.GetMotion();
            var color = fanlight.GetColorSettings();

            PrismFanlightEditorStyles.DrawSection("| Debug", () =>
            {
                _enablePreview = EditorGUILayout.Toggle("Enable Preview", _enablePreview);

                if (_enablePreview)
                {
                    EditorGUILayout.HelpBox("The preview feature can be resource-intensive, so we recommend disabling it when not needed.", MessageType.Warning);
                }

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawStat("Total Seats", audience.TotalSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Seats Per Block", audience.BlockSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Blocks", fanlight.GpuBlockCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Render Batches", "1 indirect draw");
                PrismFanlightEditorStyles.DrawStat("Backend", "GPU Indirect");
                PrismFanlightEditorStyles.DrawStat("GPU Culling", fanlight.IsCullingEnabled ? "On" : "Off");
                PrismFanlightEditorStyles.DrawStat("Visibility Update", fanlight.VisibilityUpdate.ToDisplayString());
                PrismFanlightEditorStyles.DrawStat("Animation Update", fanlight.AnimationUpdate.ToDisplayString());
                PrismFanlightEditorStyles.DrawStat("GPU Ready", fanlight.IsGpuReady ? "Yes" : "No");
                PrismFanlightEditorStyles.DrawStat("GPU Instances", fanlight.GpuSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Visible Seats", fanlight.GpuVisibleSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Culled Seats", fanlight.GpuCulledSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Culling Ratio", FormatRatio(fanlight.GpuCulledSeatCount, Mathf.Max(1, fanlight.GpuSeatCount)));
                PrismFanlightEditorStyles.DrawStat("Instance Groups", fanlight.GpuInstanceThreadGroups.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Block Groups", fanlight.GpuBlockThreadGroups.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("GPU Buffers", FormatBytes(fanlight.GpuBufferMemoryBytes));
                PrismFanlightEditorStyles.DrawStat("Motion Frequency", motion.frequency.ToString("0.###"));
                PrismFanlightEditorStyles.DrawStat("Color Mode", color.mode.ToString());
                PrismFanlightEditorStyles.DrawStat("Preview Limit", _scenePreview.PreviewSeatLimit.ToString("N0"));

                if (audience.TotalSeatCount > _scenePreview.PreviewSeatLimit)
                {
                    EditorGUILayout.HelpBox("SceneView preview is capped to keep editing responsive. Runtime rendering still uses the full seat count.", MessageType.Info);
                }
            });
        }


        private static void DrawMotionFields(SerializedProperty motion)
        {
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("frequency"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("randomPhase"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("phaseNoiseAmount"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("phaseNoiseSpeed"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Swing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("armLength"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("minAngle"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("maxAngle"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("snapAmount"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Variation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("seatJitter"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("heightJitter"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("armLengthJitter"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("axisNoiseAmount"));
            EditorGUILayout.PropertyField(motion.FindPropertyRelative("axisNoiseSpeed"));
        }

        private static void DrawColorFields(SerializedProperty color)
        {
            var mode = color.FindPropertyRelative("mode");
            var colorMode = (FanlightColorMode)mode.enumValueIndex;

            EditorGUILayout.PropertyField(mode);
            EditorGUILayout.PropertyField(color.FindPropertyRelative("primaryColor"));

            if (colorMode == FanlightColorMode.BlockGradient)
            {
                EditorGUILayout.PropertyField(color.FindPropertyRelative("secondaryColor"));
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Brightness", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(color.FindPropertyRelative("baseIntensity"));
            EditorGUILayout.PropertyField(color.FindPropertyRelative("effectIntensity"));
            EditorGUILayout.PropertyField(color.FindPropertyRelative("randomIntensity"));

            if (UsesHue(colorMode))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Hue", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(color.FindPropertyRelative("saturation"));
                EditorGUILayout.PropertyField(color.FindPropertyRelative("hueSpeed"));
                EditorGUILayout.PropertyField(color.FindPropertyRelative("randomHueAmount"));
            }

            if (UsesWave(colorMode))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Wave", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(color.FindPropertyRelative("waveOrigin"));
                EditorGUILayout.PropertyField(color.FindPropertyRelative("waveFrequency"));
                EditorGUILayout.PropertyField(color.FindPropertyRelative("waveSpeed"));
                EditorGUILayout.PropertyField(color.FindPropertyRelative("waveSharpness"));
            }
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

        private static bool UsesHue(FanlightColorMode mode) => mode is FanlightColorMode.RandomHue or FanlightColorMode.Rainbow or FanlightColorMode.Wave or FanlightColorMode.RadialWave;

        private static bool UsesWave(FanlightColorMode mode) => mode is FanlightColorMode.Wave or FanlightColorMode.RadialWave;

        private static bool IsFixedRate(SerializedProperty property)
        {
            var mode = property.FindPropertyRelative("_mode");
            return mode.enumValueIndex == (int)FanlightGpuUpdateMode.FixedRate;
        }

        private static string FormatRatio(int value, int total)
        {
            return $"{(float)value / total * 100.0f:0.0}%";
        }

        private static string FormatBytes(long bytes)
        {
            const float kb = 1024.0f;
            const float mb = kb * 1024.0f;

            if (bytes >= mb) return $"{bytes / mb:0.00} MB";
            if (bytes >= kb) return $"{bytes / kb:0.0} KB";
            return $"{bytes} B";
        }
    }
}
