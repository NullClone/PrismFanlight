using System.IO;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Rendering;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Rendering;
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

        private UnityEditor.Editor _layoutEditor;
        // private UnityEditor.Editor _materialEditor;
        // private UnityEditor.Editor _audienceMaterialEditor;

        private bool _enableGizmos = true;
        private bool _hasSelectedLayout;

        private readonly FanlightLayoutScenePreview _layoutScenePreview = new();

        private static readonly PrismFanlightSection _renderingSection = new("Rendering");
        private static readonly PrismFanlightSection _layoutSection = new("Layout");
        private static readonly PrismFanlightSection<FanlightIntentTrack> _intentSection = new("Intent");
        private static readonly PrismFanlightSection<FanlightMotionTrack> _motionSection = new("Motion");
        private static readonly PrismFanlightSection<FanlightColorTrack> _colorSection = new("Color");
        private static readonly PrismFanlightSection<FanlightIntensityTrack> _intensitySection = new("Intensity");
        private static readonly PrismFanlightSection<FanlightTempoTrack> _tempoSection = new("Tempo");
        private static readonly PrismFanlightSection<FanlightAudienceBodyTrack> _audienceSection = new("Audience");
        private static readonly PrismFanlightSection<FanlightDirectionTrack> _directionSection = new("Direction");
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

        private void OnDisable()
        {
            if (_layoutEditor != null)
            {
                DestroyImmediate(_layoutEditor);
                _layoutEditor = null;
            }

            // if (_audienceMaterialEditor != null)
            // {
            //     DestroyImmediate(_audienceMaterialEditor);
            //     _audienceMaterialEditor = null;
            // }
            //
            // if (_materialEditor != null)
            // {
            //     DestroyImmediate(_materialEditor);
            //     _materialEditor = null;
            // }
        }

        private void OnSceneGUI()
        {
            if (!_enableGizmos || _instance == null) return;

            if (_instance.LayoutAsset != null)
            {
                _hasSelectedLayout = _layoutScenePreview.Draw(_instance);
            }

            var mode = _direction.FindPropertyRelative("_mode");
            if (mode.enumValueIndex == (int)FanlightDirectionMode.WorldDirection)
            {
                var pos = _instance.transform.position;
                var rotation = Quaternion.Euler(0, _direction.FindPropertyRelative("_worldYawDegrees").floatValue, 0);
                var size = new Vector3(0.75f, 0.75f, 1f);

                // Draw Direction Gizmo

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

        private bool HasFrameBounds() => _hasSelectedLayout;

        private Bounds OnGetFrameBounds()
        {
            var layout = _instance.LayoutAsset;
            var blockIndex = FanlightLayoutScenePreview.GetSelectedBlockIndex(layout);
            var session = FanlightLayoutEditSession.Get(layout);

            if (blockIndex < 0 || session == null)
            {
                return new Bounds(_instance.transform.position, Vector3.one);
            }

            var localBounds = session.GetBlockBounds(blockIndex);
            return FanlightGeometryBuilder.TransformBounds(_instance.transform.localToWorldMatrix, localBounds);
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

            #region Rendering Section

            _renderingSection.DrawSection(() =>
            {
                DrawRenderingLayerMask(_renderingLayerMask);

                EditorGUILayout.Space();

                DrawUpdateTiming(_updateMode, "Update Mode");

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_penlightAppearanceProfile, new GUIContent("Penlight Asset"));

                var penlightAsset = _penlightAppearanceProfile.objectReferenceValue as FanlightPenlightAsset;
                if (penlightAsset == null)
                {
                    EditorGUILayout.HelpBox("Penlight Asset is required.", MessageType.Error);
                }
                else if (!penlightAsset.TryValidate(out var error))
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }

                EditorGUILayout.PropertyField(_material, new GUIContent("Penlight Material"));
                EditorGUILayout.PropertyField(_audienceMaterial, new GUIContent("Audience Material"));

                if (_material.objectReferenceValue == null || _audienceMaterial.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Penlight Material and Audience Material are required.", MessageType.Error);
                }

                EditorGUILayout.Space();

                DrawChild(_visibility, "_penlightsEnabled");
                DrawChild(_visibility, "_audienceBodiesEnabled");

                EditorGUILayout.PropertyField(_enableCulling, new GUIContent("Enable Culling"));
                if (_enableCulling.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_cullingCamera, new GUIContent("Culling Camera"));
                    }
                }

                EditorGUI.BeginChangeCheck();
                _enableGizmos = EditorGUILayout.Toggle("Enable Gizmos", _enableGizmos);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            });

            #endregion

            #region Intent Section

            _intentSection.DrawSection(() =>
            {
                DrawChild(_intent, "_energy");
                DrawChild(_intent, "_participation");
                DrawChild(_intent, "_synchronization");
                DrawChild(_intent, "_realism");
                DrawChild(_intent, "_reach");
            }, _instance);

            #endregion

            #region Motion Section

            _motionSection.DrawSection(() =>
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

                DrawChild(_motion, "_motionAmount");
                DrawChild(_motion, "_heightBias");
                DrawChild(_motion, "_sideScale");
                DrawChild(_motion, "_forwardScale");
                DrawChild(_motion, "_wristDelayRatio");
                DrawChild(_motion, "_variation");
                DrawChild(_motion, "_beatsPerCycle");
                DrawChild(_motion, "_phaseOffsetBeats");
                DrawChild(_motion, "_blockDelayXBeats");
                DrawChild(_motion, "_blockDelayYBeats");
            }, _instance);

            #endregion

            #region Color Section

            _colorSection.DrawSection(() =>
            {
                FanlightColorIntensityEditorUtility.DrawColorState(_color, _instance.LayoutAsset, true);
            }, _instance);

            #endregion

            #region Intensity Section

            _intensitySection.DrawSection(() =>
            {
                FanlightColorIntensityEditorUtility.DrawIntensityState(_intensity);
            }, _instance);

            #endregion

            #region Layout Section

            _layoutSection.DrawSection(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(_layoutAsset, new GUIContent("Layout Asset"));

                    using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
                    {
                        if (GUILayout.Button("New", GUILayout.Width(60)))
                        {
                            CreateLayoutAsset();
                        }
                    }
                }

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
                }
                else
                {
                    CreateCachedEditor(layout, null, ref _layoutEditor);

                    if (_layoutEditor != null)
                    {
                        CoreEditorUtils.DrawSplitter();
                        EditorGUILayout.Space();

                        _layoutEditor.serializedObject.Update();
                        _layoutEditor.OnInspectorGUI();
                        _layoutEditor.serializedObject.ApplyModifiedProperties();
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
                }
            });

            #endregion

            #region Tempo Section

            _tempoSection.DrawSection(() =>
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
            }, _instance);

            #endregion

            #region Audience Section

            _audienceSection.DrawSection(() =>
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
            }, _instance);

            #endregion

            #region Direction Section

            _directionSection.DrawSection(() =>
            {
                EditorGUILayout.PropertyField(_swingTarget, new GUIContent("Target"));
                EditorGUILayout.Space();

                var mode = _direction.FindPropertyRelative("_mode");

                EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));

                if (!mode.hasMultipleDifferentValues)
                {
                    if (mode.enumValueIndex == (int)FanlightDirectionMode.Target)
                    {
                        DrawChild(_direction, "_aimStrength");
                    }

                    DrawChild(_direction, "_worldYawDegrees");
                }
            }, _instance);

            #endregion

            #region Variation Section

            _variationSection.DrawSection(() =>
            {
                DrawChild(_variation, "_standingPositionSpread");
                DrawChild(_variation, "_heightVariation");
                DrawChild(_variation, "_armExtensionVariation");
                DrawChild(_variation, "_penlightDirectionSpread");
                DrawChild(_variation, "_reactionDelaySeconds");
                DrawChild(_variation, "_beatJitterBeats");
                DrawChild(_variation, "_energyResponse");
                DrawChild(_variation, "_handPositionSpread");
            }, _instance);

            #endregion

            #region Noise Section

            _noiseSection.DrawSection(() =>
            {
                EditorGUILayout.PropertyField(_globalSeed, new GUIContent("Seed"));

                DrawChild(_noise, "_phaseAmount");
                DrawChild(_noise, "_phaseRate");
                DrawChild(_noise, "_positionAmount");
                DrawChild(_noise, "_directionAmount");
                DrawChild(_noise, "_spatialRate");
                DrawChild(_noise, "_octaves");
                DrawChild(_noise, "_persistence");
            }, _instance);

            #endregion

            #region Rest Section

            _restSection.DrawSection(() =>
            {
                DrawChild(_rest, "_probability");
                DrawChild(_rest, "_motionLevel");
                DrawChild(_rest, "_cycleSeconds");
                DrawChild(_rest, "_durationSeconds");
                DrawChild(_rest, "_fadeSeconds");
                DrawChild(_rest, "_phaseRandomness");
            }, _instance);

            #endregion

            // EditorGUILayout.Space();
            // DrawMaterialEditor(_material, "Penlight", ref _materialEditor);
            // DrawMaterialEditor(_audienceMaterial, "Audience", ref _audienceMaterialEditor);

            serializedObject.ApplyModifiedProperties();
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

        /*private static void DrawMaterialEditor(SerializedProperty property, string materialName, ref UnityEditor.Editor materialEditor)
        {
            if (property.hasMultipleDifferentValues)
            {
                if (materialEditor != null)
                {
                    DestroyImmediate(materialEditor);
                    materialEditor = null;
                }

                EditorGUILayout.HelpBox($"Material Inspector is unavailable while selected objects use different {materialName} Materials.", MessageType.Info);
                return;
            }

            var material = property.objectReferenceValue as Material;
            if (material == null)
            {
                if (materialEditor != null)
                {
                    DestroyImmediate(materialEditor);
                    materialEditor = null;
                }

                return;
            }

            CreateCachedEditor(material, typeof(MaterialEditor), ref materialEditor);

            if (materialEditor is not MaterialEditor editor) return;

            EditorGUILayout.Space();

            editor.DrawHeader();
            editor.OnInspectorGUI();
        }*/

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

                Selection.activeObject = newLayoutAsset;
                FanlightLayoutIdRegistry.Invalidate();
            }
        }
    }
}
