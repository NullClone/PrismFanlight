using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(StickShow))]
    public sealed class StickShowEditor : UnityEditor.Editor
    {
        // Fields

        private const int MaxPreviewSeats = 12000;

        private static readonly Color SeatColor = new(0.1f, 0.85f, 1.0f, 0.75f);
        private static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.35f);

        private SerializedProperty _mesh;
        private SerializedProperty _material;
        private SerializedProperty _layoutPreset;
        private SerializedProperty _audience;
        private SerializedProperty _motionPreset;
        private SerializedProperty _motion;
        private SerializedProperty _colorPreset;
        private SerializedProperty _color;


        // Methods

        private void OnEnable()
        {
            _mesh = serializedObject.FindProperty("_mesh");
            _material = serializedObject.FindProperty("_material");
            _layoutPreset = serializedObject.FindProperty("_layoutPreset");
            _audience = serializedObject.FindProperty("_audience");
            _motionPreset = serializedObject.FindProperty("_motionPreset");
            _motion = serializedObject.FindProperty("_motion");
            _colorPreset = serializedObject.FindProperty("_colorPreset");
            _color = serializedObject.FindProperty("_color");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("Rendering", () =>
            {
                EditorGUILayout.PropertyField(_mesh);
                EditorGUILayout.PropertyField(_material);
            });

            DrawPresetSection(
                "Audience Layout",
                _layoutPreset,
                _audience,
                "Create Layout Preset From Current Values",
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    var stickShow = (StickShow)target;
                    CreateLayoutPreset(stickShow, stickShow.GetAudience());
                });

            DrawPresetSection(
                "Motion",
                _motionPreset,
                _motion,
                "Create Motion Preset From Current Values",
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    var stickShow = (StickShow)target;
                    CreateMotionPreset(stickShow, stickShow.GetMotion());
                });

            DrawPresetSection(
                "Color",
                _colorPreset,
                _color,
                "Create Color Preset From Current Values",
                () =>
                {
                    serializedObject.ApplyModifiedProperties();
                    var stickShow = (StickShow)target;
                    CreateColorPreset(stickShow, stickShow.GetColorSettings());
                });

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var stickShow = (StickShow)target;
                var audience = stickShow.GetAudience();

                EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Total Seats", audience.TotalSeatCount.ToString());
                EditorGUILayout.LabelField("Block Seats", audience.BlockSeatCount.ToString());

                if (audience.TotalSeatCount > MaxPreviewSeats)
                {
                    EditorGUILayout.HelpBox(
                        $"SceneView preview is capped at {MaxPreviewSeats} seats to keep editing responsive.",
                        MessageType.Info);
                }

                var motion = stickShow.GetMotion();
                var color = stickShow.GetColorSettings();
                EditorGUILayout.LabelField("Motion Frequency", motion.frequency.ToString("0.###"));
                EditorGUILayout.LabelField("Color Mode", color.mode.ToString());
            }
        }

        private static void DrawSection(string title, Action draw)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                draw();
            }
        }

        private static void DrawPresetSection(
            string title,
            SerializedProperty preset,
            SerializedProperty localSettings,
            string createButtonLabel,
            Action createPreset)
        {
            DrawSection(title, () =>
            {
                EditorGUILayout.PropertyField(preset, new GUIContent("Preset"));

                using (new EditorGUI.DisabledScope(preset.objectReferenceValue != null))
                {
                    EditorGUILayout.PropertyField(localSettings, new GUIContent("Local Settings"), true);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(createButtonLabel))
                    {
                        createPreset();
                    }

                    using (new EditorGUI.DisabledScope(preset.objectReferenceValue == null))
                    {
                        if (GUILayout.Button("Select Preset"))
                        {
                            Selection.activeObject = preset.objectReferenceValue;
                        }
                    }
                }
            });
        }

        private void OnSceneGUI()
        {
            var stickShow = (StickShow)target;
            var audience = stickShow.GetAudience();

            if (audience.TotalSeatCount <= 0 || audience.BlockSeatCount <= 0) return;

            var transform = stickShow.transform;
            DrawBlocks(transform, audience);
            DrawSeats(transform, audience);
        }

        private static void DrawBlocks(Transform transform, Audience audience)
        {
            Handles.color = BlockColor;

            for (var bx = 0; bx < audience.blockCount.x; bx++)
            {
                for (var by = 0; by < audience.blockCount.y; by++)
                {
                    var block = math.int2(bx, by);
                    var min = audience.GetPositionOnPlane(block, math.int2(0, 0)) - audience.seatPitch * 0.5f;
                    var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - math.int2(1, 1)) + audience.seatPitch * 0.5f;

                    var p0 = ToWorld(transform, math.float2(min.x, min.y));
                    var p1 = ToWorld(transform, math.float2(max.x, min.y));
                    var p2 = ToWorld(transform, math.float2(max.x, max.y));
                    var p3 = ToWorld(transform, math.float2(min.x, max.y));

                    Handles.DrawAAPolyLine(2.0f, p0, p1, p2, p3, p0);
                }
            }
        }

        private static void DrawSeats(Transform transform, Audience audience)
        {
            Handles.color = SeatColor;

            var previewCount = Mathf.Min(audience.TotalSeatCount, MaxPreviewSeats);
            var step = Mathf.Max(1, audience.TotalSeatCount / previewCount);

            for (var i = 0; i < audience.TotalSeatCount; i += step)
            {
                var (block, seat) = audience.GetCoordinatesFromIndex(i);
                var pos = audience.GetPositionOnPlane(block, seat);
                var world = ToWorld(transform, pos);
                var size = HandleUtility.GetHandleSize(world) * 0.025f;

                Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
            }
        }

        private static Vector3 ToWorld(Transform transform, float2 planePosition)
            => transform.TransformPoint(new Vector3(planePosition.x, 0.0f, planePosition.y));

        private static void CreateLayoutPreset(StickShow stickShow, Audience audience)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Audience Layout Preset",
                $"{stickShow.name} Layout Preset",
                "asset",
                "Choose where to save the audience layout preset.");

            if (string.IsNullOrEmpty(path)) return;

            var preset = CreateInstance<AudienceLayoutPreset>();
            preset.SetAudience(audience);

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(stickShow, "Assign Audience Layout Preset");
            var serializedStickShow = new SerializedObject(stickShow);
            serializedStickShow.FindProperty("_layoutPreset").objectReferenceValue = preset;
            serializedStickShow.ApplyModifiedProperties();

            Selection.activeObject = preset;
        }

        private static void CreateMotionPreset(StickShow stickShow, FanlightMotionSettings settings)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Motion Preset",
                $"{stickShow.name} Motion Preset",
                "asset",
                "Choose where to save the motion preset.");

            if (string.IsNullOrEmpty(path)) return;

            var preset = CreateInstance<FanlightMotionPreset>();
            preset.SetSettings(settings);

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(stickShow, "Assign Fanlight Motion Preset");
            var serializedStickShow = new SerializedObject(stickShow);
            serializedStickShow.FindProperty("_motionPreset").objectReferenceValue = preset;
            serializedStickShow.ApplyModifiedProperties();

            Selection.activeObject = preset;
        }

        private static void CreateColorPreset(StickShow stickShow, FanlightColorSettings settings)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Color Preset",
                $"{stickShow.name} Color Preset",
                "asset",
                "Choose where to save the color preset.");

            if (string.IsNullOrEmpty(path)) return;

            var preset = CreateInstance<FanlightColorPreset>();
            preset.SetSettings(settings);

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(stickShow, "Assign Fanlight Color Preset");
            var serializedStickShow = new SerializedObject(stickShow);
            serializedStickShow.FindProperty("_colorPreset").objectReferenceValue = preset;
            serializedStickShow.ApplyModifiedProperties();

            Selection.activeObject = preset;
        }
    }
}
