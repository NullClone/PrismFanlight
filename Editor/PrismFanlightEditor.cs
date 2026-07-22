using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PrismFanlight))]
    internal sealed class PrismFanlightEditor : UnityEditor.Editor
    {
        // Fields

        private PrismFanlight _instance;
        private SerializedProperty _penlightAppearanceProfile;
        private SerializedProperty _material;
        private SerializedProperty _audienceMaterial;
        private SerializedProperty _renderingLayerMask;
        private SerializedProperty _enableCulling;
        private SerializedProperty _cullingCamera;
        private SerializedProperty _visibilityUpdate;
        private SerializedProperty _animationUpdate;
        private SerializedProperty _layoutAsset;
        private SerializedProperty _swingTarget;
        private SerializedProperty _timeManager;
        private SerializedProperty _intent;
        private SerializedProperty _motion;
        private SerializedProperty _variation;
        private SerializedProperty _noise;
        private SerializedProperty _rest;
        private SerializedProperty _audienceBody;
        private SerializedProperty _direction;
        private SerializedProperty _palette;
        private SerializedProperty _visibility;
        private SerializedProperty _globalSeed;
        private bool _enableGizmos = true;

        private UnityEditor.Editor _layoutEditor;

        private readonly FanlightLayoutScenePreview _layoutScenePreview = new();

        private static readonly PrismFanlightSection _generalSection = new(new GUIContent("General"));
        private static readonly PrismFanlightSection _renderingSection = new(new GUIContent("Rendering"));
        private static readonly PrismFanlightSection _emissionSection = new(new GUIContent("Emission"));
        private static readonly PrismFanlightSection _layoutSection = new(new GUIContent("Layout"));
        private static readonly PrismFanlightSection _timeSection = new(new GUIContent("Time"));
        private static readonly PrismFanlightSection _variationSection = new(new GUIContent("Variation"));
        private static readonly PrismFanlightSection _noiseSection = new(new GUIContent("Noise"));
        private static readonly PrismFanlightSection _restSection = new(new GUIContent("Rest"));
        private static readonly PrismFanlightSection _audienceSection = new(new GUIContent("Audience"));


        // Methods

        private void OnEnable()
        {
            _instance = target as PrismFanlight;

            if (!_instance) return;

            _penlightAppearanceProfile = serializedObject.FindProperty(nameof(_penlightAppearanceProfile));
            _material = serializedObject.FindProperty(nameof(_material));
            _audienceMaterial = serializedObject.FindProperty(nameof(_audienceMaterial));
            _renderingLayerMask = serializedObject.FindProperty(nameof(_renderingLayerMask));
            _enableCulling = serializedObject.FindProperty(nameof(_enableCulling));
            _cullingCamera = serializedObject.FindProperty(nameof(_cullingCamera));
            _visibilityUpdate = serializedObject.FindProperty(nameof(_visibilityUpdate));
            _animationUpdate = serializedObject.FindProperty(nameof(_animationUpdate));
            _layoutAsset = serializedObject.FindProperty(nameof(_layoutAsset));
            _swingTarget = serializedObject.FindProperty(nameof(_swingTarget));
            _timeManager = serializedObject.FindProperty(nameof(_timeManager));
            _intent = serializedObject.FindProperty(nameof(_intent));
            _motion = serializedObject.FindProperty(nameof(_motion));
            _variation = serializedObject.FindProperty(nameof(_variation));
            _noise = serializedObject.FindProperty(nameof(_noise));
            _rest = serializedObject.FindProperty(nameof(_rest));
            _audienceBody = serializedObject.FindProperty(nameof(_audienceBody));
            _direction = serializedObject.FindProperty(nameof(_direction));
            _palette = serializedObject.FindProperty(nameof(_palette));
            _visibility = serializedObject.FindProperty(nameof(_visibility));
            _globalSeed = serializedObject.FindProperty(nameof(_globalSeed));
        }

        private void OnDisable()
        {
            if (_layoutEditor != null)
            {
                DestroyImmediate(_layoutEditor);
                _layoutEditor = null;
            }
        }

        private void OnSceneGUI()
        {
            if (!_enableGizmos || _instance == null) return;

            if (_instance.LayoutAsset != null)
            {
                _layoutScenePreview.Draw(_instance);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!_instance) return;

            serializedObject.Update();

            if (!SystemInfo.supportsComputeShaders)
            {
                EditorGUILayout.HelpBox("Compute shaders are not supported on this platform.", MessageType.Error);
                EditorGUILayout.Space();
            }

            DrawGeneralSection();
            DrawEmissionSection();
            DrawRenderingSection();
            DrawLayoutSection();
            DrawTimeSection();
            DrawAdvanceSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralSection()
        {
            PrismFanlightEditorStyles.DrawSection(_generalSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Intent");

                DrawSlider(_intent, "_energy", "Energy", 0f, 1f);
                DrawSlider(_intent, "_participation", "Participation", 0f, 1f);
                DrawSlider(_intent, "_synchronization", "Synchronization", 0f, 1f);
                DrawSlider(_intent, "_realism", "Realism", 0f, 1f);
                DrawSlider(_intent, "_reach", "Reach", 0f, 1f);

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Motion");

                var motionAsset = _motion.FindPropertyRelative("_motionAsset");
                EditorGUILayout.PropertyField(motionAsset, new GUIContent("Motion Asset"));

                if (motionAsset.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("A baked Motion Asset is required.", MessageType.Error);

                    using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects))
                    {
                        if (GUILayout.Button("Create Drum Motion Asset"))
                        {
                            CreateMotionAsset(motionAsset);
                        }
                    }
                }

                EditorGUILayout.Space();

                DrawSlider(_motion, "_motionAmount", "Motion Amount", 0f, 2f);
                DrawSlider(_motion, "_heightBias", "Height Bias", -1f, 1f);
                DrawSlider(_motion, "_sideScale", "Side Scale", 0f, 2f);
                DrawSlider(_motion, "_forwardScale", "Forward Scale", 0f, 2f);
                DrawSlider(_motion, "_wristDelayRatio", "Wrist Delay", 0f, 0.5f);
                DrawSlider(_motion, "_variation", "Variation", 0f, 1f);
                DrawChild(_motion, "_beatsPerCycle", "Beats Per Cycle");
                DrawChild(_motion, "_phaseOffsetBeats", "Phase Offset");

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Direction");

                var mode = _direction.FindPropertyRelative("_mode");
                EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));

                if (!mode.hasMultipleDifferentValues)
                {
                    if (mode.enumValueIndex == (int)FanlightDirectionMode.Target)
                    {
                        EditorGUILayout.PropertyField(_swingTarget, new GUIContent("Target"));
                        DrawSlider(_direction, "_aimStrength", "Aim Strength", 0f, 1f);
                        DrawChild(_direction, "_worldYawDegrees", "Fallback Yaw");
                    }
                    else
                    {
                        DrawChild(_direction, "_worldYawDegrees", "World Yaw");
                    }
                }
            });
        }

        private void DrawEmissionSection()
        {
            PrismFanlightEditorStyles.DrawSection(_emissionSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Color");

                DrawChild(_palette, "_slot1", "Slot 1");
                DrawChild(_palette, "_slot2", "Slot 2");
                DrawChild(_palette, "_slot3", "Slot 3");
                DrawChild(_palette, "_slot4", "Slot 4");
                DrawChild(_palette, "_slot5", "Slot 5");
                DrawChild(_palette, "_slot6", "Slot 6");
                DrawChild(_palette, "_globalIntensity", "Global Intensity");
                DrawSlider(_palette, "_randomIntensity", "Random Intensity", 0f, 1f);
            });
        }

        private void DrawRenderingSection()
        {
            PrismFanlightEditorStyles.DrawSection(_renderingSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Rendering");

                DrawRenderingLayerMask();

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_penlightAppearanceProfile, new GUIContent("Penlight"));

                var appearance = _penlightAppearanceProfile.objectReferenceValue as FanlightPenlightAppearanceProfile;
                if (appearance == null)
                {
                    EditorGUILayout.HelpBox("Penlight Appearance is required.", MessageType.Error);
                }
                else if (!appearance.TryValidate(out var error))
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_material);
                EditorGUILayout.PropertyField(_audienceMaterial);

                if (_material.objectReferenceValue == null || _audienceMaterial.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Penlight Material and Audience Material are required.", MessageType.Error);
                }

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("UpdateMode");

                DrawUpdateTiming(_animationUpdate, "Animation Update");
                DrawUpdateTiming(_visibilityUpdate, "Visibility Update");

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Culling");

                EditorGUILayout.PropertyField(_enableCulling, new GUIContent("Enable Culling"));
                if (_enableCulling.boolValue)
                {
                    EditorGUILayout.PropertyField(_cullingCamera, new GUIContent("Culling Camera"));
                }

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Visibility");

                DrawChild(_visibility, "_penlightsEnabled", "Enable Penlights");
                DrawChild(_visibility, "_audienceBodiesEnabled", "Enable Audience");

                EditorGUI.BeginChangeCheck();
                _enableGizmos = EditorGUILayout.Toggle("Enable Gizmos", _enableGizmos);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
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
            PrismFanlightEditorStyles.DrawSection(_layoutSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Layout");

                EditorGUILayout.PropertyField(_layoutAsset, new GUIContent("Layout Asset"));

                if (_layoutAsset.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox("Layout editing is unavailable while selected objects use different layouts.", MessageType.Info);
                    return;
                }

                var layout = _layoutAsset.objectReferenceValue as FanlightLayoutAsset;

                if (layout == null)
                {
                    if (_layoutEditor != null)
                    {
                        DestroyImmediate(_layoutEditor);
                        _layoutEditor = null;
                    }

                    _instance.SetEditorLayoutBlocked(false);

                    EditorGUILayout.HelpBox("A baked Layout Asset is required.", MessageType.Error);

                    using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
                    {
                        if (GUILayout.Button("Create Layout Asset"))
                        {
                            CreateLayoutAsset();
                        }
                    }
                }
                else
                {
                    CreateCachedEditor(layout, null, ref _layoutEditor);

                    if (_layoutEditor != null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        _layoutEditor.serializedObject.Update();
                        _layoutEditor.OnInspectorGUI();
                        _layoutEditor.serializedObject.ApplyModifiedProperties();

                        EditorGUILayout.EndVertical();
                    }

                    if (!layout.IsInitialized)
                    {
                        _instance.SetEditorLayoutBlocked(false);
                        EditorGUILayout.HelpBox("The Layout Asset is not initialized. Select it to configure the topology and bake it.", MessageType.Error);
                        return;
                    }

                    if (FanlightLayoutIdRegistry.IsDuplicate(layout))
                    {
                        _instance.SetEditorLayoutBlocked(true);
                        EditorGUILayout.HelpBox("Duplicate Layout ID detected. Rendering and baking are disabled.", MessageType.Error);
                        return;
                    }

                    _instance.SetEditorLayoutBlocked(false);

                    var session = FanlightLayoutEditSession.Get(layout);

                    if (session == null) return;

                    if (_instance.EditorPreviewContentHash != session.RuntimeLayout.ContentHash)
                    {
                        _instance.SetEditorLayoutPreview(session.RuntimeLayout, -1);
                    }

                    EditorGUILayout.Space();

                    _layoutScenePreview.EditTransforms = EditorGUILayout.Toggle(new GUIContent("Edit In Scene View"), _layoutScenePreview.EditTransforms);

                    var selected = _layoutScenePreview.GetSelectedBlockIndex(layout);

                    EditorGUILayout.Space();

                    if (selected < 0)
                    {
                        EditorGUILayout.LabelField("Selected Block", "None");
                        return;
                    }

                    var coordinates = layout.GetBlockCoordinates(selected);
                    var placement = layout.GetBlock(selected).Placement;
                    EditorGUILayout.LabelField("Selected Block", $"{coordinates.x}, {coordinates.y}");

                    using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
                    {
                        EditorGUI.BeginChangeCheck();
                        var position = EditorGUILayout.Vector3Field("Position", placement.position);
                        var rotation = EditorGUILayout.Vector3Field("Rotation", placement.eulerRotation);
                        if (EditorGUI.EndChangeCheck())
                        {
                            session.SetBlockPlacement(
                                selected,
                                new FanlightBlockPlacement
                                {
                                    position = position,
                                    eulerRotation = rotation
                                },
                                "Edit Fanlight Block Placement");
                        }

                        if (GUILayout.Button("Reset Selected Block"))
                        {
                            _layoutScenePreview.ResetSelected(layout);
                        }
                    }
                }
            });
        }

        private void CreateLayoutAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Layout Asset",
                "FanlightLayout",
                "asset",
                "Choose where to save the layout authoring asset.");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<FanlightLayoutAsset>();

            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create Fanlight Layout Asset");
            AssetDatabase.SaveAssets();

            if (_instance != null)
            {
                Undo.RecordObject(_instance, "Assign Fanlight Layout Asset");

                _instance.SetLayoutAssetForEditor(asset);

                EditorUtility.SetDirty(_instance);
            }

            Selection.activeObject = asset;
            FanlightLayoutIdRegistry.Invalidate();
        }

        private void DrawTimeSection()
        {
            PrismFanlightEditorStyles.DrawSection(_timeSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Time");

                EditorGUILayout.PropertyField(_timeManager, new GUIContent("Time Manager"));
                EditorGUILayout.PropertyField(_globalSeed, new GUIContent("Global Seed"));

                if (_timeManager.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Time Coordinator is required. Prism Fanlight does not create a fallback clock.", MessageType.Error);
                }
            });
        }

        private void DrawAdvanceSection()
        {
            PrismFanlightEditorUtility.DrawSplitter();

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Advance");
            EditorGUILayout.Space();

            PrismFanlightEditorStyles.DrawSection(_variationSection, () =>
            {
                DrawSlider(_variation, "_seatPosition", "Seat Position", 0f, 1f);
                DrawSlider(_variation, "_bodyHeight", "Body Height", 0f, 1f);
                DrawSlider(_variation, "_armLength", "Arm Length", 0f, 1f);
                DrawSlider(_variation, "_angle", "Angle", 0f, 1f);
                DrawSlider(_variation, "_directionSpread", "Direction Spread", 0f, 1f);
                DrawChild(_variation, "_reactionDelaySeconds", "Reaction Delay");
                DrawChild(_variation, "_beatJitter", "Beat Jitter");
                DrawChild(_variation, "_blockDelayXBeats", "Block Delay X");
                DrawChild(_variation, "_blockDelayYBeats", "Block Delay Y");
                DrawSlider(_variation, "_energyResponse", "Energy Response", 0f, 1f);
                DrawChild(_variation, "_speed", "Speed");
                DrawChild(_variation, "_beatReactionDelaySeconds", "Beat Reaction Delay");
                DrawSlider(_variation, "_handZone", "Hand Position Spread", 0f, 0.5f);
            });

            PrismFanlightEditorStyles.DrawSection(_noiseSection, () =>
            {
                DrawChild(_noise, "_phaseAmount", "Phase Amount");
                DrawChild(_noise, "_phaseSpeed", "Phase Speed");
                DrawChild(_noise, "_axisAmount", "Axis Amount");
                DrawChild(_noise, "_axisSpeed", "Axis Speed");
                DrawChild(_noise, "_octaves", "Octaves");
                DrawSlider(_noise, "_persistence", "Persistence", 0f, 1f);
            });

            PrismFanlightEditorStyles.DrawSection(_restSection, () =>
            {
                DrawSlider(_rest, "_probability", "Probability", 0f, 1f);
                DrawSlider(_rest, "_motionLevel", "Motion Level", 0f, 1f);
                DrawChild(_rest, "_cycleSeconds", "Cycle Seconds");
                DrawChild(_rest, "_durationSeconds", "Duration Seconds");
                DrawChild(_rest, "_fadeSeconds", "Fade Seconds");
                DrawSlider(_rest, "_phaseRandomness", "Phase Randomness", 0f, 1f);
            });

            PrismFanlightEditorStyles.DrawSection(_audienceSection, () =>
            {
                DrawChild(_audienceBody, "_height", "Height");
                DrawSlider(_audienceBody, "_heightVariation", "Height Variation", 0f, 1f);
                DrawChild(_audienceBody, "_width", "Width");
                DrawChild(_audienceBody, "_headSize", "Head Size");
                DrawSlider(_audienceBody, "_shoulderHeightRatio", "Shoulder Height", 0f, 1f);
                DrawSlider(_audienceBody, "_shoulderSideOffset", "Shoulder Offset", -1f, 1f);
                DrawChild(_audienceBody, "_armWidth", "Arm Width");
                DrawChild(_audienceBody, "_armLengthLimit", "Arm Length Limit");
                DrawChild(_audienceBody, "_upperBodyLeanMaximumRadians", "Lean Maximum");
                DrawSlider(_audienceBody, "_upperBodyLean", "Upper Body Lean", 0f, 1f);
                DrawSlider(_audienceBody, "_bounce", "Bounce", 0f, 1f);
                DrawSlider(_audienceBody, "_sway", "Sway", 0f, 1f);
                DrawChild(_audienceBody, "_motionSpeed", "Motion Speed");
                DrawSlider(_audienceBody, "_leanMotion", "Lean Motion", 0f, 1f);
            });
        }


        private static void DrawUpdateTiming(SerializedProperty timing, string label)
        {
            var mode = timing.FindPropertyRelative("_mode");

            EditorGUILayout.PropertyField(mode, new GUIContent(label));

            if (mode.enumValueIndex != (int)FanlightGpuUpdateMode.FixedRate) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(timing.FindPropertyRelative("_targetFrameRate"), new GUIContent("Target Frame Rate"));
            }
        }

        private static void CreateMotionAsset(SerializedProperty property)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Motion Asset",
                "FanlightDrumMotion",
                "asset",
                "Choose where to save the motion asset.");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<FanlightMotionAsset>();
            asset.ResetToDrum();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create Fanlight Motion Asset");
            AssetDatabase.SaveAssets();
            property.objectReferenceValue = asset;
            Selection.activeObject = asset;
        }

        private static void DrawChild(SerializedProperty parent, string propertyName, string label)
        {
            EditorGUILayout.PropertyField(parent.FindPropertyRelative(propertyName), new GUIContent(label));
        }

        private static void DrawSlider(SerializedProperty parent, string propertyName, string label, float minimum, float maximum)
        {
            var property = parent.FindPropertyRelative(propertyName);

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.Slider(label, property.floatValue, minimum, maximum);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = value;
            }

            EditorGUI.showMixedValue = false;
        }
    }
}
