using System.IO;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Rendering;

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
        private SerializedProperty _enableAudienceLod;
        private SerializedProperty _audienceLodDistance;
        private SerializedProperty _updateMode;
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
        private SerializedProperty _color;
        private SerializedProperty _intensity;
        private SerializedProperty _visibility;
        private SerializedProperty _globalSeed;

        private static readonly PrismFanlightSection _generalSection = new("General");
        private static readonly PrismFanlightSection<FanlightIntentTrack> _intentSection = new("Intent");
        private static readonly PrismFanlightSection<FanlightMotionTrack> _motionSection = new("Motion");
        private static readonly PrismFanlightSection<FanlightColorTrack> _colorSection = new("Color");
        private static readonly PrismFanlightSection<FanlightIntensityTrack> _intensitySection = new("Intensity");
        private static readonly PrismFanlightSection<FanlightAudienceBodyTrack> _audienceSection = new("Audience");
        private static readonly PrismFanlightSection<FanlightDirectionTrack> _directionSection = new("Direction");
        private static readonly PrismFanlightSection<FanlightTempoTrack> _timeSection = new("Time");
        private static readonly PrismFanlightSection<FanlightVariationTrack> _variationSection = new("Variation");
        private static readonly PrismFanlightSection<FanlightNoiseTrack> _noiseSection = new("Noise");
        private static readonly PrismFanlightSection<FanlightRestTrack> _restSection = new("Rest");


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
            _enableAudienceLod = serializedObject.FindProperty(nameof(_enableAudienceLod));
            _audienceLodDistance = serializedObject.FindProperty(nameof(_audienceLodDistance));
            _updateMode = serializedObject.FindProperty(nameof(_updateMode));
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
            _color = serializedObject.FindProperty(nameof(_color));
            _intensity = serializedObject.FindProperty(nameof(_intensity));
            _visibility = serializedObject.FindProperty(nameof(_visibility));
            _globalSeed = serializedObject.FindProperty(nameof(_globalSeed));
        }

        private void OnSceneGUI()
        {
            if (_instance == null) return;

            var mode = _direction.FindPropertyRelative("_mode");
            if (mode.enumValueIndex == (int)FanlightDirectionMode.WorldDirection)
            {
                var pos = _instance.transform.position;
                var rotation = Quaternion.Euler(0, _direction.FindPropertyRelative("_direction").floatValue, 0);
                var size = new Vector3(0.75f, 0.75f, 1f);

                Handles.color = FanlightLayoutScenePreview.SelectedColor;
                Handles.matrix = Matrix4x4.TRS(pos, rotation, size);

                var points = new Vector3[]
                {
                    new(0.0f, 0.0f, -1.0f),
                    new(0.0f, 0.5f, -1.0f),
                    new(0.0f, 0.5f, 0.0f),
                    new(0.0f, 1.0f, 0.0f),
                    new(0.0f, 0.0f, 1.0f),
                };

                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Handles.DrawLine(points[i], points[i + 1]);
                    }

                    rotation *= Quaternion.AngleAxis(180f, Vector3.forward);

                    Handles.matrix = Matrix4x4.TRS(pos, rotation, size);
                }

                Handles.matrix = Matrix4x4.identity;
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

            _generalSection.DrawSection(DrawGeneralSection);
            _motionSection.DrawSection(DrawMotionSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Motion, "_motion"));
            _intentSection.DrawSection(DrawIntentSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Intent, "_intent"));
            _colorSection.DrawSection(() =>
            {
                FanlightColorIntensityEditorUtility.DrawColorState(_color, _instance.LayoutAsset, true);
            }, _instance, menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Color, "_color"));
            _intensitySection.DrawSection(() =>
            {
                FanlightColorIntensityEditorUtility.DrawIntensityState(_intensity, _instance.LayoutAsset, true);
            }, _instance, menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Intensity, "_intensity"));
            _audienceSection.DrawSection(DrawAudienceSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.AudienceBody, "_audienceBody"));
            _directionSection.DrawSection(DrawDirectionSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Direction, "_direction"));
            _timeSection.DrawSection(DrawTimeSection, _instance);
            _variationSection.DrawSection(DrawVariationSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Variation, "_variation"));
            _noiseSection.DrawSection(DrawNoiseSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Noise, "_noise"));
            _restSection.DrawSection(DrawRestSection, _instance,
                menu => AddStateClipboardItems(menu, FanlightTimelinePatchKind.Rest, "_rest"));

            if (serializedObject.ApplyModifiedProperties())
            {
                RefreshTimelinePreview();
            }
        }

        private void DrawGeneralSection()
        {
            /*EditorGUILayout.PropertyField(_penlightAppearanceProfile, new GUIContent("Penlight Asset"));

            var penlightAsset = _penlightAppearanceProfile.objectReferenceValue as FanlightPenlightAsset;
            if (penlightAsset == null)
            {
                EditorGUILayout.HelpBox("Penlight Asset is required.", MessageType.Error);
            }
            else if (!penlightAsset.TryValidate(out var error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }*/

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_layoutAsset, new GUIContent("Layout Asset"));

                using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
                {
                    if (GUILayout.Button("New", GUILayout.Width(45)))
                    {
                        CreateLayoutAsset();
                    }

                    using (new EditorGUI.DisabledScope(_layoutAsset.objectReferenceValue == null))
                    {
                        if (GUILayout.Button("Open", GUILayout.Width(50)))
                        {
                            FanlightLayoutEditorWindow.Open(_instance);
                        }
                    }
                }
            }

            if (!_layoutAsset.hasMultipleDifferentValues)
            {
                var layout = _layoutAsset.objectReferenceValue as FanlightLayoutAsset;
                if (layout == null)
                {
                    _instance.SetEditorLayoutBlocked(false);

                    EditorGUILayout.HelpBox("Layout Asset is required.", MessageType.Error);
                }
                else
                {
                    if (!layout.IsInitialized)
                    {
                        _instance.SetEditorLayoutBlocked(false);
                        EditorGUILayout.HelpBox("The Layout Asset is not initialized. Open the Layout Editor and create a Quick Grid.", MessageType.Error);
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
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_material, new GUIContent("Penlight Material"));
            EditorGUILayout.PropertyField(_audienceMaterial, new GUIContent("Audience Material"));

            if (_material.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Penlight Material is required.", MessageType.Error);
            }

            EditorGUILayout.Space();

            DrawRenderingLayerMask(_renderingLayerMask);
            DrawUpdateTiming(_updateMode, "Update Mode");

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_cullingCamera, new GUIContent("Culling Camera"));

            if (_cullingCamera.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(_enableCulling, new GUIContent("Enable Culling"));
                EditorGUILayout.PropertyField(_enableAudienceLod, new GUIContent("Enable Distance LOD"));

                if (_enableAudienceLod.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_audienceLodDistance, new GUIContent("Distance"));
                    }
                }
            }

            DrawChild(_visibility, "_penlightsEnabled");
            DrawChild(_visibility, "_audienceBodiesEnabled");
        }

        private void DrawMotionSection()
        {
            var motionAsset = _motion.FindPropertyRelative("_motionAsset");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(motionAsset);

                if (GUILayout.Button("New", GUILayout.Width(45)))
                {
                    CreateMotionAsset(motionAsset);
                }

                using (new EditorGUI.DisabledScope(motionAsset.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Clone", GUILayout.Width(50)))
                    {
                        CloneMotionAsset(motionAsset);
                    }
                }
            }

            if (motionAsset.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("A baked Motion Asset is required.", MessageType.Error);
            }

            DrawChild(_motion, "_beatsPerCycle");
            DrawChild(_motion, "_phaseOffsetBeats");
            DrawChild(_motion, "_blockDelayXBeats");
            DrawChild(_motion, "_blockDelayYBeats");
        }

        private void DrawIntentSection()
        {
            DrawChild(_intent, "_energy");
            DrawChild(_intent, "_participation");
            DrawChild(_intent, "_synchronization");
            DrawChild(_intent, "_transitionScatter");
        }

        private void DrawAudienceSection()
        {
            DrawChild(_audienceBody, "_height");
            DrawChild(_audienceBody, "_width");
            DrawChild(_audienceBody, "_headSize");
            DrawChild(_audienceBody, "_armWidth");
            DrawChild(_audienceBody, "_armLengthLimit");
            DrawChild(_audienceBody, "_shoulderHeightRatio");
            DrawChild(_audienceBody, "_shoulderSideOffset");
            DrawChild(_audienceBody, "_bounce");
            DrawChild(_audienceBody, "_sway");
        }

        private void DrawDirectionSection()
        {
            var mode = _direction.FindPropertyRelative("_mode");

            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));

            if (!mode.hasMultipleDifferentValues)
            {
                if (mode.enumValueIndex == (int)FanlightDirectionMode.WorldDirection)
                {
                    DrawChild(_direction, "_direction");
                }
                else
                {
                    EditorGUILayout.PropertyField(_swingTarget, new GUIContent("Target"));
                }
            }
        }

        private void DrawTimeSection()
        {
            EditorGUILayout.PropertyField(_timeManager, new GUIContent("Time Manager"));

            if (_timeManager.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Time Coordinator is required. Prism Fanlight does not create a fallback clock.", MessageType.Error);
            }

            if (_instance.TimeFault == FanlightShowTimeFault.TempoConflict)
            {
                EditorGUILayout.HelpBox("Tempo Conflict: two or more Tempo Tracks are active for this Prism Fanlight.", MessageType.Error);
            }
            else if (_instance.TimeFault != FanlightShowTimeFault.None)
            {
                EditorGUILayout.HelpBox($"Time Fault: {_instance.TimeFault}", MessageType.Error);
            }

            if (!string.IsNullOrEmpty(_instance.SequenceFault))
            {
                EditorGUILayout.HelpBox($"Sequence Field Conflict: {_instance.SequenceFault}", MessageType.Error);
            }
        }

        private void DrawVariationSection()
        {
            DrawChild(_variation, "_standingPositionSpread");
            DrawChild(_variation, "_heightVariation");
            DrawChild(_variation, "_armExtensionVariation");
            DrawChild(_variation, "_penlightDirectionSpread");
            DrawChild(_variation, "_reactionDelaySeconds");
            DrawChild(_variation, "_beatJitterBeats");
            DrawChild(_variation, "_energyResponse");
            DrawChild(_variation, "_handPositionSpread");
        }

        private void DrawNoiseSection()
        {
            EditorGUILayout.PropertyField(_globalSeed, new GUIContent("Seed"));

            DrawChild(_noise, "_phaseAmount");
            DrawChild(_noise, "_phaseRate");
            DrawChild(_noise, "_positionAmount");
            DrawChild(_noise, "_directionAmount");
            DrawChild(_noise, "_spatialRate");
            DrawChild(_noise, "_octaves");
            DrawChild(_noise, "_persistence");
        }

        private void DrawRestSection()
        {
            DrawChild(_rest, "_probability");
            DrawChild(_rest, "_motionLevel");
            DrawChild(_rest, "_cycleSeconds");
            DrawChild(_rest, "_durationSeconds");
            DrawChild(_rest, "_fadeSeconds");
            DrawChild(_rest, "_phaseRandomness");
        }

        private void RefreshTimelinePreview()
        {
            if (Application.isPlaying || !AnimationMode.InAnimationMode()) return;

            var timeline = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;

            if (timeline == null || director == null || !director.playableGraph.IsValid()) return;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not FanlightTimelineTrackAsset && track is not FanlightTempoTrack) continue;

                var binding = director.GetGenericBinding(track);

                for (var i = 0; i < targets.Length; i++)
                {
                    if (binding != targets[i]) continue;

                    TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
                    return;
                }
            }
        }

        private void AddStateClipboardItems(GenericMenu menu, FanlightTimelinePatchKind kind, string propertyPath)
        {
            menu.AddSeparator("");

            if (targets.Length == 1)
            {
                menu.AddItem(new GUIContent("Copy"), false, () => FanlightStateClipboard.Copy(targets, kind, propertyPath));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            if (FanlightStateClipboard.CanPaste(kind))
            {
                menu.AddItem(new GUIContent("Paste"), false, () =>
                {
                    if (FanlightStateClipboard.Paste(targets, kind, propertyPath))
                    {
                        RefreshTimelinePreview();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }
        }


        private void CreateMotionAsset(SerializedProperty motionAsset)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Motion Asset",
                "New Fanlight Motion",
                "asset",
                "Choose where to save the motion authoring asset.");

            if (!string.IsNullOrEmpty(path))
            {
                var newMotionAsset = CreateInstance<FanlightMotionAsset>();
                AssetDatabase.CreateAsset(newMotionAsset, path);
                AssetDatabase.SaveAssets();

                if (_instance != null)
                {
                    Undo.RecordObject(_instance, "Assign Fanlight Motion Asset");

                    motionAsset.objectReferenceValue = newMotionAsset;

                    EditorUtility.SetDirty(_instance);
                }

                Selection.activeObject = newMotionAsset;
            }
        }

        private void CloneMotionAsset(SerializedProperty motionAsset)
        {
            var originalMotionAsset = motionAsset.objectReferenceValue as FanlightMotionAsset;
            if (originalMotionAsset != null)
            {
                var originalPath = AssetDatabase.GetAssetPath(originalMotionAsset);
                var defaultPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
                var defaultName = Path.GetFileNameWithoutExtension(defaultPath);

                var path = EditorUtility.SaveFilePanelInProject(
                    "Clone Fanlight Motion Asset",
                    defaultName,
                    "asset",
                    "Choose where to save the motion authoring asset.");

                if (!string.IsNullOrEmpty(path))
                {
                    var newMotionAsset = Instantiate(originalMotionAsset);
                    AssetDatabase.CreateAsset(newMotionAsset, path);
                    AssetDatabase.SaveAssets();

                    if (_instance != null)
                    {
                        Undo.RecordObject(_instance, "Assign Fanlight Motion Asset");

                        motionAsset.objectReferenceValue = newMotionAsset;

                        EditorUtility.SetDirty(_instance);
                    }

                    Selection.activeObject = newMotionAsset;
                }
            }
        }

        private void CreateLayoutAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Layout Asset",
                "New Fanlight Layout",
                "asset",
                "Choose where to save the layout authoring asset.");

            if (!string.IsNullOrEmpty(path))
            {
                var newLayoutAsset = CreateInstance<FanlightLayoutAsset>();

                AssetDatabase.CreateAsset(newLayoutAsset, path);
                AssetDatabase.SaveAssets();

                if (_instance != null)
                {
                    Undo.RecordObject(_instance, "Assign Fanlight Layout Asset");

                    _instance.SetLayoutAssetForEditor(newLayoutAsset);

                    EditorUtility.SetDirty(_instance);
                }

                FanlightLayoutIdRegistry.Invalidate();
                FanlightLayoutEditorWindow.Open(_instance);
            }
        }


        private static void DrawRenderingLayerMask(SerializedProperty property)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;

            EditorGUI.BeginChangeCheck();

#if UNITY_6000_0_OR_NEWER
            var mask = EditorGUILayout.RenderingLayerMaskField(new GUIContent("Rendering Layer"), (uint)property.longValue);
#else
            var renderingLayerMaskNames = GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames;

            if (renderingLayerMaskNames == null || renderingLayerMaskNames.Length == 0) return;

            var mask = (uint)EditorGUILayout.MaskField(new GUIContent("Rendering Layer"), (int)property.longValue, renderingLayerMaskNames);
#endif

            if (EditorGUI.EndChangeCheck())
            {
                property.longValue = mask;
            }
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

        private static void DrawChild(SerializedProperty parent, string propertyName)
        {
            EditorGUILayout.PropertyField(parent.FindPropertyRelative(propertyName));
        }
    }
}
