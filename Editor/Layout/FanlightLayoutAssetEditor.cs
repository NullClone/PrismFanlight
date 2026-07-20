using System;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FanlightLayoutAsset))]
    internal sealed class FanlightLayoutAssetEditor : UnityEditor.Editor
    {
        // Fields

        private FanlightLayoutAsset _instance;
        private Vector2Int _seatPerBlock;
        private Vector2 _seatPitch;
        private Vector2Int _blockCount;
        private Vector2 _aisleWidth;


        // Methods

        private void OnEnable()
        {
            _instance = target as FanlightLayoutAsset;

            if (_instance == null) return;

            _seatPerBlock = new Vector2Int(_instance.SeatPerBlock.x, _instance.SeatPerBlock.y);
            _seatPitch = new Vector2(_instance.SeatPitch.x, _instance.SeatPitch.y);
            _blockCount = new Vector2Int(_instance.BlockCount.x, _instance.BlockCount.y);
            _aisleWidth = new Vector2(_instance.AisleWidth.x, _instance.AisleWidth.y);
        }

        public override void OnInspectorGUI()
        {
            if (_instance == null) return;

            EditorGUILayout.Space();

            _blockCount = Vector2Int.Max(EditorGUILayout.Vector2IntField("Block Count", _blockCount), Vector2Int.one);
            _seatPerBlock = Vector2Int.Max(EditorGUILayout.Vector2IntField("Seats Per Block", _seatPerBlock), Vector2Int.one);
            _seatPitch = Vector2.Max(EditorGUILayout.Vector2Field("Seat Pitch", _seatPitch), Vector2.one * 0.001f);
            _aisleWidth = Vector2.Max(EditorGUILayout.Vector2Field("Aisle Width", _aisleWidth), Vector2.zero);

            var validCount = TryGetTotalSeatCount(out var totalSeats);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total Seats", validCount ? totalSeats.ToString("N0") : "Unsupported");
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects || !validCount))
            {
                var initialized = _instance.IsInitialized;
                var label = initialized ? "Rebuild Layout & Bake" : "Initialize & Bake";

                if (GUILayout.Button(label))
                {
                    var confirmed = !initialized || EditorUtility.DisplayDialog(
                        "Rebuild Fanlight Layout",
                        "Rebuilding replaces the Stable Layout ID, all Block IDs, all Stable Seat IDs, and all Block Placements. The existing Bake Artifact SubAsset will be updated.",
                        "Rebuild & Bake",
                        "Cancel");

                    if (confirmed)
                    {
                        var instance = _instance;
                        var seatPerBlock = new int2(_seatPerBlock.x, _seatPerBlock.y);
                        var seatPitch = new float2(_seatPitch.x, _seatPitch.y);
                        var blockCount = new int2(_blockCount.x, _blockCount.y);
                        var aisleWidth = new float2(_aisleWidth.x, _aisleWidth.y);
                        EditorApplication.delayCall += () => RebuildAndBake(
                            instance,
                            seatPerBlock,
                            seatPitch,
                            blockCount,
                            aisleWidth);
                    }

                    GUIUtility.ExitGUI();
                }
            }

            if (!_instance.IsInitialized)
            {
                EditorGUILayout.HelpBox("Configure the topology, then initialize and bake the Layout Asset.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stable Identity", _instance.LayoutId.Value);
            EditorGUILayout.LabelField("Content Hash", _instance.ContentHash.ToString("X16"));
            EditorGUILayout.Space();

            var embedded = FanlightLayoutEditSession.IsEmbeddedBake(_instance, _instance.ActiveBake);
            EditorGUILayout.LabelField("Storage", embedded ? "Embedded" : "Not Embedded");

            if (FanlightLayoutIdRegistry.IsDuplicate(_instance))
            {
                EditorGUILayout.HelpBox("Duplicate Layout ID detected. Rendering and baking are disabled.", MessageType.Error);
                return;
            }

            var session = FanlightLayoutEditSession.Get(_instance);

            if (session == null) return;

            EditorGUILayout.LabelField("Status", session.HasCurrentBake ? "Current" : "Bake Required");

            using (new EditorGUI.DisabledScope(Application.isPlaying || serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("Bake Dirty Blocks"))
                {
                    var instance = _instance;
                    EditorApplication.delayCall += () => Bake(instance);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void RebuildAndBake(
            FanlightLayoutAsset instance,
            int2 seatPerBlock,
            float2 seatPitch,
            int2 blockCount,
            float2 aisleWidth)
        {
            if (instance == null) return;

            try
            {
                Undo.RecordObject(instance, instance.IsInitialized ? "Rebuild Fanlight Layout" : "Initialize Fanlight Layout");

                instance.Rebuild(seatPerBlock, seatPitch, blockCount, aisleWidth);

                EditorUtility.SetDirty(instance);
                FanlightLayoutEditSession.Reset(instance);
                FanlightLayoutIdRegistry.Invalidate();

                var session = FanlightLayoutEditSession.Get(instance);

                if (session == null) throw new InvalidOperationException("The rebuilt Layout Asset is invalid.");

                session.Bake();
                session.ApplyPreviewToAllInstances(-1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Fanlight Layout Rebuild Failed", exception.Message, "OK");
            }
        }

        private static void Bake(FanlightLayoutAsset instance)
        {
            if (instance == null) return;
            FanlightLayoutEditSession.Get(instance)?.Bake();
        }

        private bool TryGetTotalSeatCount(out long totalSeats)
        {
            try
            {
                totalSeats = checked(
                    (long)_blockCount.x
                    * _blockCount.y
                    * _seatPerBlock.x
                    * _seatPerBlock.y);
                return totalSeats is > 0 and <= int.MaxValue;
            }
            catch (OverflowException)
            {
                totalSeats = 0;
                return false;
            }
        }
    }
}
