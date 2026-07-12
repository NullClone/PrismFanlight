using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PrismFanlight))]
    public sealed class PrismFanlightEditor : UnityEditor.Editor
    {
        // Fields

        private PrismFanlight _instance;

        private SerializedProperty _mesh;
        private SerializedProperty _material;
        private SerializedProperty _audienceMaterial;
        private SerializedProperty _computeShader;
        private SerializedProperty _renderingLayerMask;
        private SerializedProperty _enableCulling;
        private SerializedProperty _visibilityUpdate;
        private SerializedProperty _animationUpdate;
        private SerializedProperty _tempo;
        private SerializedProperty _cullingCamera;
        private SerializedProperty _seatLayout;
        private SerializedProperty _motionPreset;
        private SerializedProperty _motion;
        private SerializedProperty _swingTarget;
        private SerializedProperty _colorPreset;
        private SerializedProperty _color;
        private SerializedProperty _audienceSettings;
        private SerializedProperty _lod;
        private SerializedProperty _random;

        private bool _enableGizmos = true;


        private void OnEnable()
        {
            _instance = target as PrismFanlight;

            if (!_instance) return;

            _mesh = serializedObject.FindProperty(nameof(_mesh));
            _material = serializedObject.FindProperty(nameof(_material));
            _audienceMaterial = serializedObject.FindProperty(nameof(_audienceMaterial));
            _computeShader = serializedObject.FindProperty(nameof(_computeShader));
            _renderingLayerMask = serializedObject.FindProperty(nameof(_renderingLayerMask));
            _enableCulling = serializedObject.FindProperty(nameof(_enableCulling));
            _visibilityUpdate = serializedObject.FindProperty(nameof(_visibilityUpdate));
            _animationUpdate = serializedObject.FindProperty(nameof(_animationUpdate));
            _tempo = serializedObject.FindProperty(nameof(_tempo));
            _cullingCamera = serializedObject.FindProperty(nameof(_cullingCamera));
            _seatLayout = serializedObject.FindProperty(nameof(_seatLayout));
            _motionPreset = serializedObject.FindProperty(nameof(_motionPreset));
            _motion = serializedObject.FindProperty(nameof(_motion));
            _swingTarget = serializedObject.FindProperty(nameof(_swingTarget));
            _colorPreset = serializedObject.FindProperty(nameof(_colorPreset));
            _color = serializedObject.FindProperty(nameof(_color));
            _audienceSettings = serializedObject.FindProperty(nameof(_audienceSettings));
            _lod = serializedObject.FindProperty(nameof(_lod));
            _random = serializedObject.FindProperty(nameof(_random));
        }

        private void OnSceneGUI()
        {
            if (!_enableGizmos) return;

            if (target is PrismFanlight fanlight)
            {
                new PrismFanlightScenePreview().Draw(fanlight);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!_instance) return;

            serializedObject.Update();

            FanlightTimelineOverrideInspectorState.Update(_instance);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_computeShader, new GUIContent("Compute Shader"));
            }

            if (_computeShader.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign PrismFanlightIndirect.compute to generate instance data on the GPU.", MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_mesh, new GUIContent("Mesh"));

            if (_mesh.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Stick Mesh is required.", MessageType.Error);
            }

            if (_material.objectReferenceValue == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Assign a Material to render the penlight.", MessageType.Error);
            }

            if (_audienceMaterial.objectReferenceValue == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Assign a Material to render the audience.", MessageType.Error);
            }

            DrawGeneralSection();
            DrawLayoutSection();
            DrawMotionSection();
            DrawAudienceSection();
            DrawLodSection();
            DrawColorSection();
            DrawTempoSection();
            DrawRandomSection();
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        public override bool RequiresConstantRepaint()
        {
            return _instance != null && FanlightTimelineOverrideInspectorState.IsInspecting(_instance);
        }

        private void DrawPropertyField(SerializedProperty property, GUIContent label, bool includeChildren = false)
        {
            if (property == null) return;

            var overridden = IsTimelineOverridden(property);

            if (!overridden)
            {
                EditorGUILayout.PropertyField(property, label, includeChildren);
            }
            else
            {
                PrismFanlightEditorStyles.DrawOverride(property, label, includeChildren);
            }
        }

        private bool IsTimelineOverridden(params SerializedProperty[] properties)
        {
            if (serializedObject.isEditingMultipleObjects) return false;

            foreach (var property in properties)
            {
                if (property != null && FanlightTimelineOverrideInspectorState.IsOverridden(property.propertyPath)) return true;
            }

            return false;
        }

        private readonly struct TimelineOverrideColorScope : IDisposable
        {
            private readonly Color _previousColor;


            public TimelineOverrideColorScope(bool overridden)
            {
                _previousColor = GUI.color;

                if (overridden)
                {
                    GUI.color = Color.Lerp(_previousColor, AnimationMode.animatedPropertyColor, 0.45f);
                }
            }

            public void Dispose()
            {
                GUI.color = _previousColor;
            }
        }

        private void DrawGeneralSection()
        {
            PrismFanlightEditorStyles.DrawSection("| General", () =>
            {
                DrawRenderingLayerMask();

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

        private void DrawRenderingLayerMask()
        {
            EditorGUI.BeginChangeCheck();

#if UNITY_6000_0_OR_NEWER
            var mask = EditorGUILayout.RenderingLayerMaskField(new GUIContent("Rendering Layer"), (uint)_renderingLayerMask.longValue);
#else
            string[] renderingLayerMaskNames = null;

            if (GraphicsSettings.currentRenderPipeline != null)
            {
                renderingLayerMaskNames = GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames;
            }

            if (renderingLayerMaskNames == null || renderingLayerMaskNames.Length == 0) return;
            
            var mask = (uint)EditorGUILayout.MaskField(new GUIContent("Rendering Layer"), (int)_renderingLayerMask.longValue, renderingLayerMaskNames);
#endif

            if (EditorGUI.EndChangeCheck())
            {
                _renderingLayerMask.longValue = mask;
            }
        }

        private void DrawLayoutSection()
        {
            PrismFanlightEditorStyles.DrawSection("| Layout", () =>
            {
                EditorGUILayout.LabelField("Blocks", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("blockCount"), new GUIContent("Block Count"));
                EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("aisleWidth"), new GUIContent("Aisle Width"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Seats", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("seatPerBlock"), new GUIContent("Seats Per Block"));
                EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("seatPitch"), new GUIContent("Seat Pitch"));

                EditorGUILayout.Space();

                PrismFanlightEditorStyles.DrawSubGroupLabel("Block Placement");

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    PrismFanlightScenePreview.EditBlockTransforms = EditorGUILayout.Toggle(
                        new GUIContent("Edit In Scene View", "Select a block in the Scene view, then move and rotate it with Unity handles."),
                        PrismFanlightScenePreview.EditBlockTransforms);
                }

                var layout = _instance.GetSeatLayout();
                var totalBlockCount = layout.TotalBlockCount;
                EnsureBlockTransformProperties(_seatLayout, totalBlockCount);

                var selected = PrismFanlightScenePreview.SelectedBlockIndex;
                if (selected >= totalBlockCount)
                {
                    PrismFanlightScenePreview.SelectedBlockIndex = -1;
                    selected = -1;
                }

                if (layout.NeedsBake)
                {
                    EditorGUILayout.HelpBox("Layout placement has unbaked changes. Bake before entering Play mode for the fastest runtime path.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Layout placement is baked. Runtime will upload the baked seat and block data directly.", MessageType.None);
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Bake Layout"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            _instance.BakeSeatLayoutForEditor();
                            EditorUtility.SetDirty(_instance);
                            serializedObject.Update();
                        }

                        if (GUILayout.Button("Clear Bake"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            _instance.ClearSeatLayoutBakeForEditor();
                            EditorUtility.SetDirty(_instance);
                            serializedObject.Update();
                        }
                    }
                }

                EditorGUILayout.Space();

                if (selected < 0)
                {
                    EditorGUILayout.LabelField("Selected Block", "None");
                    return;
                }

                var block = layout.GetBlockCoordinates(selected);
                EditorGUILayout.LabelField("Selected Block", $"{block.x}, {block.y}");

                var transformsProperty = EnsureBlockTransformProperties(_seatLayout, totalBlockCount);
                var transformProperty = transformsProperty.GetArrayElementAtIndex(selected);

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    EditorGUILayout.PropertyField(transformProperty.FindPropertyRelative("position"), new GUIContent("Position"));
                    EditorGUILayout.PropertyField(transformProperty.FindPropertyRelative("eulerRotation"), new GUIContent("Rotation"));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Reset Selected"))
                        {
                            ResetBlockTransform(transformProperty);
                        }

                        if (GUILayout.Button("Reset All"))
                        {
                            for (var i = 0; i < transformsProperty.arraySize; i++)
                            {
                                ResetBlockTransform(transformsProperty.GetArrayElementAtIndex(i));
                            }
                        }
                    }
                }
            });
        }

        private void DrawTempoSection()
        {
            PrismFanlightEditorStyles.DrawSection("| Tempo", () =>
            {
                EditorGUILayout.Space();
                DrawPropertyField(_tempo.FindPropertyRelative("bpm"), new GUIContent("BPM"));
                DrawPropertyField(_tempo.FindPropertyRelative("beatsPerBar"), new GUIContent("Beats Per Bar"));
                EditorGUILayout.Space();
                DrawPropertyField(_tempo.FindPropertyRelative("clockSource"), new GUIContent("Clock Source"));

                var clockSource = (FanlightTempoClockSource)_tempo.FindPropertyRelative("clockSource").enumValueIndex;

                if (clockSource == FanlightTempoClockSource.AudioSourceTime)
                {
                    var audioSourceProp = _tempo.FindPropertyRelative("audioSource");
                    DrawPropertyField(audioSourceProp, new GUIContent("Audio Source"));
                    if (audioSourceProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Assign an AudioSource with a clip for BPM sync.", MessageType.Info);
                    }
                }
                else if (clockSource == FanlightTempoClockSource.ManualTime)
                {
                    DrawPropertyField(_tempo.FindPropertyRelative("manualTime"), new GUIContent("Manual Time"));
                }

                DrawPropertyField(_tempo.FindPropertyRelative("offsetSeconds"), new GUIContent("Offset"));
                DrawPropertyField(_tempo.FindPropertyRelative("latencyCompensationSeconds"), new GUIContent("Latency Compensation"));
                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Beat Sync");

                var beatSync = _motion.FindPropertyRelative("beatSync");
                DrawPropertyField(beatSync.FindPropertyRelative("beatsPerSwing"), new GUIContent("Beats Per Swing"));
                DrawPropertyField(beatSync.FindPropertyRelative("beatPhaseOffset"), new GUIContent("Phase Offset"));
                DrawPropertyField(beatSync.FindPropertyRelative("downbeatAccent"), new GUIContent("Downbeat Accent"));
                EditorGUILayout.Space();
                DrawPropertyField(beatSync.FindPropertyRelative("beatReactionDelay"), new GUIContent("Reaction Delay"));
                DrawPropertyField(beatSync.FindPropertyRelative("beatSeatJitter"), new GUIContent("Seat Jitter"));
                DrawPropertyField(beatSync.FindPropertyRelative("beatBlockDelay"), new GUIContent("Block Delay"));
            });
        }

        private void DrawMotionSection()
        {
            DrawPresetSection(
                "| Motion",
                _motionPreset,
                () => DrawMotionFields(_motion),
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    PrismFanlightPresetUtility.CreateMotionPreset(_instance, _instance.GetMotionSettings());
                });
        }

        private void DrawMotionFields(SerializedProperty motion)
        {
            var swingProp = motion.FindPropertyRelative("swing");
            var directionProp = motion.FindPropertyRelative("direction");
            var noiseProp = motion.FindPropertyRelative("noise");
            var humanProp = motion.FindPropertyRelative("human");

            PrismFanlightEditorStyles.DrawSubGroupLabel("Swing");

            DrawPropertyField(swingProp.FindPropertyRelative("randomPhase"), new GUIContent("Phase Randomness"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Arm");

            var armMinProp = swingProp.FindPropertyRelative("armLengthMin");
            var armMaxProp = swingProp.FindPropertyRelative("armLengthMax");
            using (new TimelineOverrideColorScope(IsTimelineOverridden(armMinProp, armMaxProp)))
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Arm Length");

                var armMin = armMinProp.floatValue;
                var armMax = armMaxProp.floatValue;

                armMin = EditorGUILayout.FloatField(armMin);
                EditorGUILayout.MinMaxSlider(ref armMin, ref armMax, 0f, 2f);
                armMax = EditorGUILayout.FloatField(armMax);

                armMin = math.round(armMin * 100f) / 100f;
                armMax = math.round(armMax * 100f) / 100f;

                armMinProp.floatValue = armMin;
                armMaxProp.floatValue = armMax;
            }

            DrawPropertyField(humanProp.FindPropertyRelative("armLengthJitter"), new GUIContent("Arm Length Jitter"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Angle");

            var minAngleProp = swingProp.FindPropertyRelative("minAngle");
            var maxAngleProp = swingProp.FindPropertyRelative("maxAngle");
            using (new TimelineOverrideColorScope(IsTimelineOverridden(minAngleProp, maxAngleProp)))
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Angle Range");

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

            DrawPropertyField(swingProp.FindPropertyRelative("angleNoise"), new GUIContent("Angle Variation"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Shape");

            DrawPropertyField(swingProp.FindPropertyRelative("crispness"), new GUIContent("Crispness"));
            DrawPropertyField(swingProp.FindPropertyRelative("peakHold"), new GUIContent("Peak Hold"));
            DrawPropertyField(swingProp.FindPropertyRelative("followThrough"), new GUIContent("Follow Through", "Wrist trails the arm and curls back during the stroke (vertical pattern)."));
            DrawPropertyField(swingProp.FindPropertyRelative("lean"), new GUIContent("Lean"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Pattern");

            var horizontalRatioProp = swingProp.FindPropertyRelative("horizontalRatio");
            DrawPropertyField(horizontalRatioProp,
                new GUIContent("Horizontal Ratio", "Fraction of the crowd doing the side-to-side wave instead of the fore-aft swing."));

            if (horizontalRatioProp.floatValue > 0f)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawPropertyField(swingProp.FindPropertyRelative("wristSwingSpeed"),
                        new GUIContent("Wrist Speed", "How much faster the wrist flick is than the arm sway."));
                    DrawPropertyField(swingProp.FindPropertyRelative("wristSwingAngle"),
                        new GUIContent("Wrist Swing", "Amplitude of the fast wrist flick (radians)."));
                }
            }

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Direction");

            var swingModeProp = directionProp.FindPropertyRelative("swingMode");
            DrawPropertyField(swingModeProp, new GUIContent("Swing Mode"));
            var swingMode = (FanlightSwingMode)swingModeProp.enumValueIndex;

            if (swingMode == FanlightSwingMode.WorldDirection)
            {
                DrawPropertyField(directionProp.FindPropertyRelative("swingYaw"), new GUIContent("Direction"));
            }
            else
            {
                EditorGUILayout.PropertyField(_swingTarget, new GUIContent("Swing Target"));
                var aimStrengthProp = directionProp.FindPropertyRelative("aimStrength");
                DrawPropertyField(aimStrengthProp, new GUIContent("Direction Strength"));

                if (aimStrengthProp.floatValue < 1f)
                {
                    DrawPropertyField(directionProp.FindPropertyRelative("swingYaw"), new GUIContent("Fallback Direction"));
                }
            }

            DrawPropertyField(directionProp.FindPropertyRelative("directionSpread"), new GUIContent("Direction Spread"));
            EditorGUILayout.Space();

            PrismFanlightEditorStyles.DrawSubGroupLabel("Noise");

            DrawPropertyField(noiseProp.FindPropertyRelative("phaseIrregularity"), new GUIContent("Phase Irregularity"));
            DrawPropertyField(noiseProp.FindPropertyRelative("phaseIrregularitySpeed"), new GUIContent("Irregularity Speed"));
            DrawPropertyField(noiseProp.FindPropertyRelative("axisNoiseAmount"), new GUIContent("Axis Drift"));
            DrawPropertyField(noiseProp.FindPropertyRelative("axisNoiseSpeed"), new GUIContent("Drift Speed"));
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(true))
            {
                DrawPropertyField(noiseProp.FindPropertyRelative("noiseOctaves"), new GUIContent("Octaves"));
                DrawPropertyField(noiseProp.FindPropertyRelative("noiseDetail"), new GUIContent("Detail"));
            }

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Feel");

            DrawPropertyField(humanProp.FindPropertyRelative("enthusiasm"), new GUIContent("Enthusiasm"));
            DrawPropertyField(humanProp.FindPropertyRelative("enthusiasmVariation"), new GUIContent("Variation"));
            DrawPropertyField(humanProp.FindPropertyRelative("lazyFanRatio"), new GUIContent("Lazy Fan Ratio"));
            EditorGUILayout.Space();
            DrawPropertyField(humanProp.FindPropertyRelative("seatJitter"), new GUIContent("Seat Jitter"));
            DrawPropertyField(humanProp.FindPropertyRelative("heightJitter"), new GUIContent("Height Jitter"));
            EditorGUILayout.Space();
            DrawPropertyField(humanProp.FindPropertyRelative("reactionDelay"), new GUIContent("Reaction Delay"));
            DrawPropertyField(humanProp.FindPropertyRelative("speedVariation"), new GUIContent("Speed Variation"));

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Rest");

            DrawPropertyField(humanProp.FindPropertyRelative("restProbability"), new GUIContent("Probability"));
            DrawPropertyField(humanProp.FindPropertyRelative("restMotionLevel"), new GUIContent("Motion Level"));
            EditorGUILayout.Space();
            DrawPropertyField(humanProp.FindPropertyRelative("restCycleDuration"),
                new GUIContent("Cycle Duration", "How often the rest cycle repeats (seconds)"));
            DrawPropertyField(humanProp.FindPropertyRelative("restDuration"),
                new GUIContent("Rest Duration", "How long each rest lasts within the cycle (seconds)"));
            DrawPropertyField(humanProp.FindPropertyRelative("restFadeDuration"),
                new GUIContent("Fade Duration", "Time to fade in and out of the rest state (seconds)"));
            DrawPropertyField(humanProp.FindPropertyRelative("restPhaseRandomness"),
                new GUIContent("Phase Randomness", "Per-fan variation in rest cycle timing"));
        }

        private void DrawColorSection()
        {
            DrawPresetSection(
                "| Color",
                _colorPreset,
                () => DrawColorFields(_color),
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    PrismFanlightPresetUtility.CreateColorPreset(_instance, _instance.GetColorSettings());
                });
        }

        private void DrawColorFields(SerializedProperty color)
        {
            var mode = color.FindPropertyRelative("mode");
            var colorMode = (FanlightColorMode)mode.enumValueIndex;

            DrawPropertyField(mode, null);

            switch (colorMode)
            {
                case FanlightColorMode.Single:
                    DrawPropertyField(color.FindPropertyRelative("primaryColor"), new GUIContent("Color"));
                    DrawIntensityField(color);
                    break;

                case FanlightColorMode.Random:
                    DrawPropertyField(color.FindPropertyRelative("paletteColors"), new GUIContent("Palette"));
                    DrawIntensityField(color);
                    DrawPropertyField(color.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
                    break;

                case FanlightColorMode.Gradient:
                    DrawPropertyField(color.FindPropertyRelative("primaryColor"), new GUIContent("Start Color"));
                    DrawPropertyField(color.FindPropertyRelative("secondaryColor"), new GUIContent("End Color"));
                    DrawIntensityField(color);
                    DrawPropertyField(color.FindPropertyRelative("randomIntensity"), new GUIContent("Random Intensity"));
                    break;
            }
        }

        private void DrawIntensityField(SerializedProperty color)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brightness", EditorStyles.boldLabel);
            DrawPropertyField(color.FindPropertyRelative("intensity"), new GUIContent("Intensity"));
        }

        private void DrawAudienceSection()
        {
            PrismFanlightEditorStyles.DrawSection("| Audience", () =>
            {
                var enabledProp = _audienceSettings.FindPropertyRelative("enabled");
                DrawPropertyField(enabledProp, new GUIContent("Enable"));

                if (!enabledProp.boolValue) return;

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Body");
                DrawPropertyField(_audienceSettings.FindPropertyRelative("bodyHeight"), new GUIContent("Height"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("bodyHeightJitter"), new GUIContent("Height Jitter"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("bodyWidth"), new GUIContent("Width"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("headSize"), new GUIContent("Head Size"));

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Arm");
                DrawPropertyField(_audienceSettings.FindPropertyRelative("shoulderHeight"), new GUIContent("Shoulder Height"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("shoulderOffset"), new GUIContent("Shoulder Offset"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("armWidth"), new GUIContent("Arm Width"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("armLengthLimit"),
                    new GUIContent("Arm Length Limit", "Caps the generated arm length before the penlight is placed at the hand."));

                EditorGUILayout.Space();
                DrawHandZoneFields(_audienceSettings.FindPropertyRelative("handZone"));

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Upper Body");
                DrawPropertyField(_audienceSettings.FindPropertyRelative("upperBodyLean"), new GUIContent("Body Lean"));
                DrawPropertyField(_audienceSettings.FindPropertyRelative("upperBodyLeanMax"), new GUIContent("Lean Max"));

                var motion = _audienceSettings.FindPropertyRelative("motion");
                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Motion");
                DrawPropertyField(motion.FindPropertyRelative("bodyBounce"), new GUIContent("Body Bounce"));
                DrawPropertyField(motion.FindPropertyRelative("bodySway"), new GUIContent("Body Sway"));
                DrawPropertyField(motion.FindPropertyRelative("bodyMotionSpeed"), new GUIContent("Body Speed"));
                EditorGUILayout.Space();
                DrawPropertyField(motion.FindPropertyRelative("upperBodyLeanMotion"), new GUIContent("Lean Motion"));
            });
        }

        private void DrawLodSection()
        {
            PrismFanlightEditorStyles.DrawSection("| LOD", () =>
            {
                var audienceLod = _lod.FindPropertyRelative("enableAudienceDistanceLod");
                EditorGUILayout.PropertyField(audienceLod, new GUIContent("Audience Distance LOD"));

                if (!audienceLod.boolValue) return;

                EditorGUILayout.PropertyField(_lod.FindPropertyRelative("audienceVisibleDistance"), new GUIContent("Audience Distance"));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(_lod.FindPropertyRelative("audienceFadeRange"), new GUIContent("Fade Range"));
                }
            });
        }

        private void DrawRandomSection()
        {
            PrismFanlightEditorStyles.DrawSection("| Random", () =>
            {
                EditorGUILayout.PropertyField(_random.FindPropertyRelative("deterministic"), new GUIContent("Deterministic"));
                EditorGUILayout.PropertyField(_random.FindPropertyRelative("globalSeed"), new GUIContent("Global Seed"));
            });
        }

        private void DrawHandZoneFields(SerializedProperty handZone)
        {
            PrismFanlightEditorStyles.DrawSubGroupLabel("Hand Zone");

            var zoneProp = handZone.FindPropertyRelative("zone");
            EditorGUI.BeginChangeCheck();
            DrawPropertyField(zoneProp, new GUIContent("Zone"));

            if (EditorGUI.EndChangeCheck())
            {
                ApplyHandZonePreset(handZone, (FanlightHandZone)zoneProp.enumValueIndex);
            }

            DrawPropertyField(handZone.FindPropertyRelative("heightOffset"), new GUIContent("Height Offset"));
            DrawPropertyField(handZone.FindPropertyRelative("forwardOffset"), new GUIContent("Forward Offset"));
            DrawPropertyField(handZone.FindPropertyRelative("reachScale"), new GUIContent("Reach Scale"));
            DrawPropertyField(handZone.FindPropertyRelative("variation"), new GUIContent("Variation"));
        }

        private static void ApplyHandZonePreset(SerializedProperty handZone, FanlightHandZone zone)
        {
            var preset = FanlightHandZoneSettings.Preset(zone);
            handZone.FindPropertyRelative("heightOffset").floatValue = preset.heightOffset;
            handZone.FindPropertyRelative("forwardOffset").floatValue = preset.forwardOffset;
            handZone.FindPropertyRelative("reachScale").floatValue = preset.reachScale;
            handZone.FindPropertyRelative("variation").floatValue = preset.variation;
        }

        private void DrawDebugSection()
        {
            var diagnostics = _instance.GetDiagnostics();

            PrismFanlightEditorStyles.DrawSection("| Debug", () =>
            {
                if (!SystemInfo.supportsComputeShaders)
                {
                    EditorGUILayout.HelpBox("Compute shaders are not supported on this platform; preview is unavailable.", MessageType.Warning);
                }

                _enableGizmos = EditorGUILayout.Toggle("Enable Gizmos", _enableGizmos);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
                PrismFanlightEditorStyles.DrawStat("GPU Ready", diagnostics.IsGpuReady ? "Yes" : "No");
                PrismFanlightEditorStyles.DrawStat("Total Seats", diagnostics.TotalSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Visible Seats", diagnostics.VisibleSeatCount.ToString("N0"));
                PrismFanlightEditorStyles.DrawStat("Blocks", diagnostics.BlockCount.ToString("N0"));
            });
        }

        private static SerializedProperty EnsureBlockTransformProperties(SerializedProperty seatLayout, int count)
        {
            var transformsProperty = seatLayout.FindPropertyRelative("blockTransforms");

            if (transformsProperty.arraySize != count)
            {
                var oldSize = transformsProperty.arraySize;
                transformsProperty.arraySize = count;

                for (var i = oldSize; i < count; i++)
                {
                    ResetBlockTransform(transformsProperty.GetArrayElementAtIndex(i));
                }
            }

            return transformsProperty;
        }

        private static void ResetBlockTransform(SerializedProperty transformProperty)
        {
            transformProperty.FindPropertyRelative("position").vector3Value = Vector3.zero;
            transformProperty.FindPropertyRelative("eulerRotation").vector3Value = Vector3.zero;
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

        private static bool IsFixedRate(SerializedProperty property)
        {
            return property.FindPropertyRelative("_mode").enumValueIndex == (int)FanlightGpuUpdateMode.FixedRate;
        }
    }
}
