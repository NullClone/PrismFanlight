using PrismFanlight;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StickShow))]
public sealed class StickShowEditor : Editor
{
    private const int MaxPreviewSeats = 12000;

    private static readonly Color SeatColor = new(0.1f, 0.85f, 1.0f, 0.75f);
    private static readonly Color BlockColor = new(0.1f, 0.85f, 1.0f, 0.35f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

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

            if (GUILayout.Button("Create Layout Preset From Current Values"))
            {
                CreateLayoutPreset(stickShow, audience);
            }
        }
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
                var max = audience.GetPositionOnPlane(block, audience.seatPerBlock - 1) + audience.seatPitch * 0.5f;

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
}
