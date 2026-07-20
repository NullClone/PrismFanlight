using PrismFanlight.Authoring;
using PrismFanlight.Core;
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
        private SerializedProperty _penlightAppearanceProfile;
        private SerializedProperty _material;
        private SerializedProperty _audienceMaterial;
        private SerializedProperty _computeShader;
        private SerializedProperty _renderingLayerMask;
        private SerializedProperty _enableCulling;
        private SerializedProperty _cullingCamera;
        private SerializedProperty _visibilityUpdate;
        private SerializedProperty _animationUpdate;
        private SerializedProperty _seatLayout;
        private SerializedProperty _layoutAsset;
        private SerializedProperty _swingTarget;
        private SerializedProperty _timeCoordinator;
        private SerializedProperty _intent;
        private SerializedProperty _gesture;
        private SerializedProperty _pose;
        private SerializedProperty _variation;
        private SerializedProperty _noise;
        private SerializedProperty _rest;
        private SerializedProperty _audienceBody;
        private SerializedProperty _direction;
        private SerializedProperty _palette;
        private SerializedProperty _visibility;
        private SerializedProperty _globalSeed;
        private bool _enableGizmos = true;

        private readonly FanlightLayoutScenePreview _layoutScenePreview = new();

        private static readonly PrismFanlightSection _renderingSection = new(new GUIContent("Rendering"));
        private static readonly PrismFanlightSection _generalSection = new(new GUIContent("General"));
        private static readonly PrismFanlightSection _layoutSection = new(new GUIContent("Layout"));
        private static readonly PrismFanlightSection _timeSection = new(new GUIContent("Time"));
        private static readonly PrismFanlightSection _advanceSection = new(new GUIContent("Advance"));


        // Methods

        private void OnEnable()
        {
            _instance = target as PrismFanlight;

            if (!_instance) return;

            _penlightAppearanceProfile = serializedObject.FindProperty(nameof(_penlightAppearanceProfile));
            _material = serializedObject.FindProperty(nameof(_material));
            _audienceMaterial = serializedObject.FindProperty(nameof(_audienceMaterial));
            _computeShader = serializedObject.FindProperty(nameof(_computeShader));
            _renderingLayerMask = serializedObject.FindProperty(nameof(_renderingLayerMask));
            _enableCulling = serializedObject.FindProperty(nameof(_enableCulling));
            _cullingCamera = serializedObject.FindProperty(nameof(_cullingCamera));
            _visibilityUpdate = serializedObject.FindProperty(nameof(_visibilityUpdate));
            _animationUpdate = serializedObject.FindProperty(nameof(_animationUpdate));
            _seatLayout = serializedObject.FindProperty(nameof(_seatLayout));
            _layoutAsset = serializedObject.FindProperty(nameof(_layoutAsset));
            _swingTarget = serializedObject.FindProperty(nameof(_swingTarget));
            _timeCoordinator = serializedObject.FindProperty(nameof(_timeCoordinator));
            _intent = serializedObject.FindProperty(nameof(_intent));
            _gesture = serializedObject.FindProperty(nameof(_gesture));
            _pose = serializedObject.FindProperty(nameof(_pose));
            _variation = serializedObject.FindProperty(nameof(_variation));
            _noise = serializedObject.FindProperty(nameof(_noise));
            _rest = serializedObject.FindProperty(nameof(_rest));
            _audienceBody = serializedObject.FindProperty(nameof(_audienceBody));
            _direction = serializedObject.FindProperty(nameof(_direction));
            _palette = serializedObject.FindProperty(nameof(_palette));
            _visibility = serializedObject.FindProperty(nameof(_visibility));
            _globalSeed = serializedObject.FindProperty(nameof(_globalSeed));
        }

        private void OnSceneGUI()
        {
            if (!_enableGizmos || _instance == null) return;

            if (_instance.LayoutAsset != null)
            {
                _layoutScenePreview.Draw(_instance);
            }
            else
            {
                new PrismFanlightScenePreview().Draw(_instance);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!_instance) return;

            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_computeShader);
            }

            if (!SystemInfo.supportsComputeShaders)
            {
                EditorGUILayout.HelpBox("Compute shaders are not supported on this platform.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawRenderingSection();
            DrawGeneralSection();
            DrawLayoutSection();
            DrawTimeSection();
            DrawAdvanceSection();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _enableGizmos = EditorGUILayout.Toggle("Enable Gizmos", _enableGizmos);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRenderingSection()
        {
            PrismFanlightEditorStyles.DrawSection(_renderingSection, () =>
            {
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

                DrawRenderingLayerMask();

                EditorGUILayout.Space();

                DrawUpdateTiming(_animationUpdate, "Animation Update");
                DrawUpdateTiming(_visibilityUpdate, "Visibility Update");

                EditorGUILayout.Space();
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

        private void DrawGeneralSection()
        {
            PrismFanlightEditorStyles.DrawSection(_generalSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Main");

                DrawSlider(_intent, "_energy", "Energy", 0f, 1f);
                DrawSlider(_intent, "_participation", "Participation", 0f, 1f);
                DrawSlider(_intent, "_synchronization", "Synchronization", 0f, 1f);
                DrawSlider(_intent, "_realism", "Realism", 0f, 1f);
                DrawSlider(_intent, "_reach", "Reach", 0f, 1f);

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Gesture");

                DrawChild(_gesture, "_beatsPerCycle", "Beats Per Cycle");
                DrawChild(_gesture, "_phaseOffsetBeats", "Phase Offset");
                DrawSlider(_gesture, "_holdRatio", "Hold", 0f, 1f);
                DrawSlider(_gesture, "_crispness", "Crispness", 0f, 1f);
                DrawSlider(_gesture, "_followThrough", "Follow Through", 0f, 1f);
                DrawChild(_gesture, "_downbeatAccent", "Downbeat Accent");

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Color");

                DrawChild(_palette, "_slot1", "Slot 1");
                DrawChild(_palette, "_slot2", "Slot 2");
                DrawChild(_palette, "_slot3", "Slot 3");
                DrawChild(_palette, "_slot4", "Slot 4");
                DrawChild(_palette, "_slot5", "Slot 5");
                DrawChild(_palette, "_slot6", "Slot 6");
                DrawChild(_palette, "_globalIntensity", "Global Intensity");
                DrawSlider(_palette, "_randomIntensity", "Random Intensity", 0f, 1f);

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

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Visibility");

                DrawChild(_visibility, "_penlightsEnabled", "Penlights");
                DrawChild(_visibility, "_audienceBodiesEnabled", "Audience");
            });
        }

        private void DrawLayoutSection()
        {
            PrismFanlightEditorStyles.DrawSection(_layoutSection, () =>
            {
                EditorGUILayout.PropertyField(_layoutAsset, new GUIContent("Layout Asset"));

                if (_layoutAsset.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox("Layout editing is unavailable while selected objects use different layouts.", MessageType.Info);
                    return;
                }

                var layout = _layoutAsset.objectReferenceValue as FanlightLayoutAsset;
                if (layout != null)
                {
                    DrawLayoutAssetControls(layout);
                    return;
                }

                _instance.SetEditorLayoutBlocked(false);

                DrawEmbeddedLayoutControls();
            });
        }

        private void DrawLayoutAssetControls(FanlightLayoutAsset layout)
        {
            if (FanlightLayoutIdRegistry.IsDuplicate(layout))
            {
                _instance.SetEditorLayoutBlocked(true);
                EditorGUILayout.HelpBox("Duplicate Layout ID detected. Rendering and baking are disabled.", MessageType.Error);
                return;
            }

            _instance.SetEditorLayoutBlocked(false);
            if (!layout.IsInitialized)
            {
                EditorGUILayout.HelpBox("The Layout Asset is not initialized.", MessageType.Error);
                return;
            }

            var session = FanlightLayoutEditSession.Get(layout);
            if (session == null) return;
            if (_instance.EditorPreviewContentHash != session.RuntimeLayout.ContentHash)
            {
                _instance.SetEditorLayoutPreview(session.RuntimeLayout, -1);
            }

            EditorGUILayout.Space();
            _layoutScenePreview.EditTransforms = EditorGUILayout.Toggle(
                new GUIContent("Edit In Scene View"),
                _layoutScenePreview.EditTransforms);
            EditorGUILayout.LabelField(
                "Bake Status",
                session.DirtyBlockCount == 0 && layout.HasCompatibleBake ? "Current" : "Bake Required");

            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("Bake Dirty Blocks...")) session.BakeWithSaveDialog();
            }

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

                if (GUILayout.Button("Reset Selected Block")) _layoutScenePreview.ResetSelected(layout);
            }
        }

        private void DrawEmbeddedLayoutControls()
        {
            EditorGUILayout.HelpBox("Create a Layout Asset to use stable IDs, block editing and partial preview.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("Create Layout Asset...")) FanlightLayoutCreationWindow.ShowFor(_instance);
            }

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Embedded Layout");
            EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("blockCount"), new GUIContent("Block Count"));
            EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("aisleWidth"), new GUIContent("Aisle Width"));
            EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("seatPerBlock"), new GUIContent("Seats Per Block"));
            EditorGUILayout.PropertyField(_seatLayout.FindPropertyRelative("seatPitch"), new GUIContent("Seat Pitch"));

            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
            {
                PrismFanlightScenePreview.EditBlockTransforms = EditorGUILayout.Toggle(
                    new GUIContent("Edit In Scene View"),
                    PrismFanlightScenePreview.EditBlockTransforms);
            }

            var layout = _instance.GetSeatLayout();
            var totalBlockCount = layout.TotalBlockCount;
            var transforms = EnsureBlockTransformProperties(_seatLayout, totalBlockCount);
            var selected = PrismFanlightScenePreview.SelectedBlockIndex;
            if (selected >= totalBlockCount)
            {
                PrismFanlightScenePreview.SelectedBlockIndex = -1;
                selected = -1;
            }

            EditorGUILayout.LabelField("Bake Status", layout.NeedsBake ? "Bake Required" : "Current");
            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
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

            var coordinates = layout.GetBlockCoordinates(selected);
            EditorGUILayout.LabelField("Selected Block", $"{coordinates.x}, {coordinates.y}");
            var transformProperty = transforms.GetArrayElementAtIndex(selected);
            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
            {
                EditorGUILayout.PropertyField(transformProperty.FindPropertyRelative("position"), new GUIContent("Position"));
                EditorGUILayout.PropertyField(transformProperty.FindPropertyRelative("eulerRotation"), new GUIContent("Rotation"));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset Selected")) ResetBlockTransform(transformProperty);
                    if (GUILayout.Button("Reset All"))
                    {
                        for (var i = 0; i < transforms.arraySize; i++)
                        {
                            ResetBlockTransform(transforms.GetArrayElementAtIndex(i));
                        }
                    }
                }
            }
        }

        private void DrawTimeSection()
        {
            PrismFanlightEditorStyles.DrawSection(_timeSection, () =>
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.PropertyField(_timeCoordinator, new GUIContent("Time Coordinator"));
                }

                EditorGUILayout.PropertyField(_globalSeed, new GUIContent("Global Seed"));

                if (_timeCoordinator.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Time Coordinator is required. Prism Fanlight does not create a fallback clock.", MessageType.Error);
                }
            });
        }

        private void DrawAdvanceSection()
        {
            PrismFanlightEditorStyles.DrawSection(_advanceSection, () =>
            {
                PrismFanlightEditorStyles.DrawSubGroupLabel("Pose");

                DrawChild(_pose, "_handZone", "Hand Zone");
                DrawChild(_pose, "_handHeightOffset", "Hand Height Offset");
                DrawChild(_pose, "_handForwardOffset", "Hand Forward Offset");
                DrawChild(_pose, "_handReachScale", "Hand Reach Scale");
                DrawChild(_pose, "_armLengthMinimum", "Arm Length Minimum");
                DrawChild(_pose, "_armLengthMaximum", "Arm Length Maximum");
                DrawChild(_pose, "_angleMinimumRadians", "Angle Minimum");
                DrawChild(_pose, "_angleMaximumRadians", "Angle Maximum");
                DrawSlider(_pose, "_horizontalRatio", "Horizontal Ratio", 0f, 1f);
                DrawChild(_pose, "_wristFrequencyMultiplier", "Wrist Frequency");
                DrawChild(_pose, "_wristAngleRadians", "Wrist Angle");
                DrawSlider(_pose, "_bodyLean", "Body Lean", -1f, 1f);

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Variation");

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
                DrawSlider(_variation, "_handZone", "Hand Zone", 0f, 0.5f);

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Noise");

                DrawChild(_noise, "_phaseAmount", "Phase Amount");
                DrawChild(_noise, "_phaseSpeed", "Phase Speed");
                DrawChild(_noise, "_axisAmount", "Axis Amount");
                DrawChild(_noise, "_axisSpeed", "Axis Speed");
                DrawChild(_noise, "_octaves", "Octaves");
                DrawSlider(_noise, "_persistence", "Persistence", 0f, 1f);

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Rest");

                DrawSlider(_rest, "_probability", "Probability", 0f, 1f);
                DrawSlider(_rest, "_motionLevel", "Motion Level", 0f, 1f);
                DrawChild(_rest, "_cycleSeconds", "Cycle Seconds");
                DrawChild(_rest, "_durationSeconds", "Duration Seconds");
                DrawChild(_rest, "_fadeSeconds", "Fade Seconds");
                DrawSlider(_rest, "_phaseRandomness", "Phase Randomness", 0f, 1f);

                EditorGUILayout.Space();
                PrismFanlightEditorStyles.DrawSubGroupLabel("Audience Body");

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

        private static SerializedProperty EnsureBlockTransformProperties(SerializedProperty seatLayout, int count)
        {
            var transforms = seatLayout.FindPropertyRelative("blockTransforms");
            if (transforms.arraySize == count) return transforms;

            var oldSize = transforms.arraySize;
            transforms.arraySize = count;
            for (var i = oldSize; i < count; i++)
            {
                ResetBlockTransform(transforms.GetArrayElementAtIndex(i));
            }

            return transforms;
        }

        private static void ResetBlockTransform(SerializedProperty transformProperty)
        {
            transformProperty.FindPropertyRelative("position").vector3Value = Vector3.zero;
            transformProperty.FindPropertyRelative("eulerRotation").vector3Value = Vector3.zero;
        }
    }
}
