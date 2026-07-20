using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightLayoutCreationWindow : EditorWindow
    {
        private PrismFanlight _target;
        private Vector2Int _seatPerBlock = new(8, 12);
        private Vector2 _seatPitch = new(0.4f, 0.8f);
        private Vector2Int _blockCount = new(7, 3);
        private Vector2 _aisleWidth = new(0.7f, 1.2f);

        public static void ShowFor(PrismFanlight target)
        {
            var window = CreateInstance<FanlightLayoutCreationWindow>();
            window.titleContent = new GUIContent("Create Fanlight Layout");
            window._target = target;
            window.minSize = new Vector2(360f, 220f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Topology", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Topology is immutable after creation. Seat add, delete, split, merge and asset duplication remain disabled until PF-OPEN-006 is resolved.", MessageType.Info);
            _blockCount = EditorGUILayout.Vector2IntField("Block Count", _blockCount);
            _seatPerBlock = EditorGUILayout.Vector2IntField("Seats Per Block", _seatPerBlock);
            _seatPitch = EditorGUILayout.Vector2Field("Seat Pitch", _seatPitch);
            _aisleWidth = EditorGUILayout.Vector2Field("Aisle Width", _aisleWidth);

            _blockCount = Vector2Int.Max(_blockCount, Vector2Int.one);
            _seatPerBlock = Vector2Int.Max(_seatPerBlock, Vector2Int.one);
            _seatPitch = Vector2.Max(_seatPitch, Vector2.one * 0.001f);
            _aisleWidth = Vector2.Max(_aisleWidth, Vector2.zero);

            var totalSeats = (long)_blockCount.x * _blockCount.y * _seatPerBlock.x * _seatPerBlock.y;
            EditorGUILayout.LabelField("Total Seats", totalSeats.ToString("N0"));

            using (new EditorGUI.DisabledScope(totalSeats <= 0 || totalSeats > int.MaxValue))
            {
                if (GUILayout.Button("Create Layout Asset")) CreateAsset((int)totalSeats);
            }
        }

        private void CreateAsset(int totalSeats)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Fanlight Layout Asset",
                "FanlightLayout",
                "asset",
                "Choose where to save the immutable-topology layout asset.");
            if (string.IsNullOrEmpty(path)) return;

            var totalBlocks = _blockCount.x * _blockCount.y;
            var blockIds = new string[totalBlocks];
            for (var i = 0; i < blockIds.Length; i++) blockIds[i] = Guid.NewGuid().ToString("N");
            var seatIds = CreateSeatIds(totalSeats);

            var asset = CreateInstance<FanlightLayoutAsset>();
            asset.Initialize(
                Guid.NewGuid().ToString("N"),
                math.int2(_seatPerBlock.x, _seatPerBlock.y),
                math.float2(_seatPitch.x, _seatPitch.y),
                math.int2(_blockCount.x, _blockCount.y),
                math.float2(_aisleWidth.x, _aisleWidth.y),
                blockIds,
                seatIds);
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create Fanlight Layout Asset");
            AssetDatabase.SaveAssets();

            if (_target != null)
            {
                Undo.RecordObject(_target, "Assign Fanlight Layout Asset");
                _target.SetLayoutAssetForEditor(asset);
                EditorUtility.SetDirty(_target);
            }

            Selection.activeObject = asset;
            FanlightLayoutIdRegistry.Invalidate();
            Close();
        }

        private static ulong[] CreateSeatIds(int count)
        {
            var values = new ulong[count];
            var used = new HashSet<ulong>();
            var bytes = new byte[8];
            using var random = RandomNumberGenerator.Create();
            for (var i = 0; i < values.Length; i++)
            {
                ulong value;
                do
                {
                    random.GetBytes(bytes);
                    value = BitConverter.ToUInt64(bytes, 0);
                } while (value == 0UL || !used.Add(value));

                values[i] = value;
            }

            return values;
        }
    }

    [CustomEditor(typeof(FanlightLayoutAsset))]
    internal sealed class FanlightLayoutAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var layout = (FanlightLayoutAsset)target;
            EditorGUILayout.LabelField("Stable Identity", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(layout.LayoutId.Value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("Content Hash", layout.ContentHash.ToString("X16"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Immutable Topology", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Blocks", $"{layout.BlockCount.x} × {layout.BlockCount.y}");
            EditorGUILayout.LabelField("Seats Per Block", $"{layout.SeatPerBlock.x} × {layout.SeatPerBlock.y}");
            EditorGUILayout.LabelField("Total Seats", layout.TotalSeatCount.ToString("N0"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Active Artifact", layout.ActiveBake, typeof(FanlightLayoutBakeArtifact), false);
            EditorGUILayout.LabelField("Status", layout.HasValidBake ? "Current" : "Bake Required");

            if (FanlightLayoutIdRegistry.IsDuplicate(layout))
            {
                EditorGUILayout.HelpBox("Duplicate layout ID detected. This asset is invalid; baking and rendering are disabled until PF-OPEN-006 defines duplication semantics.", MessageType.Error);
            }
        }
    }

    [InitializeOnLoad]
    internal static class FanlightLayoutIdRegistry
    {
        private static readonly Dictionary<string, int> Counts = new(StringComparer.Ordinal);
        private static bool _valid;

        static FanlightLayoutIdRegistry()
        {
            EditorApplication.projectChanged += Invalidate;
            EditorApplication.playModeStateChanged += _ => ScheduleSceneValidation();
            ScheduleSceneValidation();
        }

        public static void Invalidate()
        {
            _valid = false;
            ScheduleSceneValidation();
        }

        public static bool IsDuplicate(FanlightLayoutAsset layout)
        {
            if (layout == null || !layout.LayoutId.IsValid) return true;
            EnsureBuilt();
            return Counts.TryGetValue(layout.LayoutId.Value, out var count) && count > 1;
        }

        private static void EnsureBuilt()
        {
            if (_valid) return;
            Counts.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:FanlightLayoutAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var layout = AssetDatabase.LoadAssetAtPath<FanlightLayoutAsset>(path);
                if (layout == null || !layout.LayoutId.IsValid) continue;
                Counts.TryGetValue(layout.LayoutId.Value, out var count);
                Counts[layout.LayoutId.Value] = count + 1;
            }

            _valid = true;
        }

        private static void ScheduleSceneValidation()
        {
            EditorApplication.delayCall -= ValidateSceneInstances;
            EditorApplication.delayCall += ValidateSceneInstances;
        }

        private static void ValidateSceneInstances()
        {
            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                var layout = fanlight.LayoutAsset;
                fanlight.SetEditorLayoutBlocked(layout != null && IsDuplicate(layout));
            }
        }
    }
}
