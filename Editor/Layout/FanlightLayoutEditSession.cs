using System;
using System.Collections;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Rendering;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    [InitializeOnLoad]
    internal sealed class FanlightLayoutEditSession
    {
        // Fields

        private static readonly Dictionary<int, FanlightLayoutEditSession> Sessions = new();

        private readonly FanlightCompiledLayout _compiled;
        private readonly FanlightSeatData[] _gpuSeats;
        private readonly ulong[] _stableSeatIds;
        private readonly FanlightBakedBlockData[] _gpuBlocks;
        private readonly FanlightBoundsTree _boundsTree;
        private readonly FanlightHashTree _hashTree;
        private readonly Vector3[][] _corners;
        private readonly BitArray _dirtyBlocks;
        private int _dirtyBlockCount;
        private FanlightRuntimeLayout _runtimeLayout;


        // Properties

        internal FanlightLayoutAsset Source { get; }

        internal FanlightRuntimeLayout RuntimeLayout => _runtimeLayout;

        internal int DirtyBlockCount => _dirtyBlockCount;

        internal bool HasEmbeddedBake => IsEmbeddedBake(Source, Source.ActiveBake);

        internal bool HasCurrentBake => HasEmbeddedBake && Source.HasValidBake && DirtyBlockCount == 0;


        // Methods

        static FanlightLayoutEditSession()
        {
            Undo.undoRedoPerformed += ResetAll;
            AssemblyReloadEvents.beforeAssemblyReload += ResetAll;
        }

        private FanlightLayoutEditSession(FanlightLayoutAsset source)
        {
            Source = source;
            _compiled = new FanlightCompiledLayout(source);
            _gpuSeats = new FanlightSeatData[source.TotalSeatCount];
            _stableSeatIds = new ulong[source.TotalSeatCount];
            _gpuBlocks = new FanlightBakedBlockData[source.TotalBlockCount];
            _boundsTree = new FanlightBoundsTree(source.TotalBlockCount);
            _hashTree = new FanlightHashTree(source.TotalBlockCount);
            _corners = new Vector3[source.TotalBlockCount][];
            _dirtyBlocks = new BitArray(source.TotalBlockCount);

            for (var i = 0; i < source.TotalBlockCount; i++)
            {
                ConvertBlock(i);

                _boundsTree.Update(i, _compiled.Blocks[i].localBounds);
                _hashTree.Update(i, _compiled.Blocks[i].contentHash);
                _corners[i] = BuildCorners(i);
                _dirtyBlocks[i] = !IsBlockCurrent(i);

                if (_dirtyBlocks[i]) _dirtyBlockCount++;
            }

            RefreshRuntimeLayout();
        }

        internal static FanlightLayoutEditSession Get(FanlightLayoutAsset source)
        {
            if (source == null || !source.IsInitialized) return null;

            var key = source.GetInstanceID();

            if (!Sessions.TryGetValue(key, out var session) || session.Source != source)
            {
                session = new FanlightLayoutEditSession(source);
                Sessions[key] = session;
            }

            return session;
        }

        internal static void ResetAll()
        {
            Sessions.Clear();

            if (Application.isPlaying) return;

            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                fanlight.ClearEditorLayoutPreview();
            }
        }

        internal static void Reset(FanlightLayoutAsset source)
        {
            if (source == null) return;

            Sessions.Remove(source.GetInstanceID());

            if (Application.isPlaying) return;

            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                if (fanlight.LayoutAsset == source) fanlight.ClearEditorLayoutPreview();
            }
        }

        internal Vector3[] GetCorners(int blockIndex) => _corners[blockIndex];

        internal Bounds GetBlockBounds(int blockIndex) => _compiled.Blocks[blockIndex].localBounds;

        internal void QueryVisible(Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            _boundsTree.Query(planes, localToWorld, results);
        }

        internal bool SetBlockPlacement(int blockIndex, FanlightBlockPlacement placement, string undoName)
        {
            Undo.RecordObject(Source, undoName);

            if (!Source.SetBlockPlacement(blockIndex, placement)) return false;

            EditorUtility.SetDirty(Source);

            _compiled.CompileBlock(blockIndex);

            ConvertBlock(blockIndex);

            _boundsTree.Update(blockIndex, _compiled.Blocks[blockIndex].localBounds);
            _hashTree.Update(blockIndex, _compiled.Blocks[blockIndex].contentHash);

            WriteCorners(blockIndex, _corners[blockIndex]);

            if (!_dirtyBlocks[blockIndex])
            {
                _dirtyBlocks[blockIndex] = true;
                _dirtyBlockCount++;
            }

            RefreshRuntimeLayout();
            ApplyPreviewToAllInstances(blockIndex);

            return true;
        }

        internal bool Bake()
        {
            if (FanlightLayoutIdRegistry.IsDuplicate(Source))
            {
                EditorUtility.DisplayDialog("Invalid Fanlight Layout", "Duplicate layout ID detected. Baking is disabled.", "OK");
                return false;
            }

            _compiled.SetSummary(_boundsTree.Root, ComputeLayoutHash());

            try
            {
                var path = AssetDatabase.GetAssetPath(Source);

                if (string.IsNullOrEmpty(path) || AssetDatabase.LoadMainAssetAtPath(path) != Source)
                {
                    throw new InvalidOperationException("The Layout Asset must be saved as the main object of an .asset file before baking.");
                }

                var artifact = FindEmbeddedBake(path);
                if (artifact == null)

                {
                    artifact = ScriptableObject.CreateInstance<FanlightLayoutBakeArtifact>();
                    artifact.name = $"{Source.name} Bake Artifact";
                    artifact.hideFlags = HideFlags.NotEditable;
                    AssetDatabase.AddObjectToAsset(artifact, Source);
                }

                artifact.name = $"{Source.name} Bake Artifact";
                artifact.hideFlags = HideFlags.NotEditable;
                artifact.Initialize(
                    Source.LayoutId.Value,
                    _compiled.ContentHash,
                    _compiled.LocalBounds,
                    _compiled.Seats,
                    _compiled.Blocks);
                Source.SetActiveBake(artifact);

                EditorUtility.SetDirty(artifact);
                EditorUtility.SetDirty(Source);
                AssetDatabase.SaveAssets();
                EditorApplication.RepaintProjectWindow();

                for (var i = 0; i < _dirtyBlocks.Length; i++) _dirtyBlocks[i] = false;

                _dirtyBlockCount = 0;
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Fanlight Layout Bake Failed", exception.Message, "OK");
                return false;
            }
        }

        internal void ApplyPreviewToAllInstances(int changedBlockIndex)
        {
            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                if (fanlight.LayoutAsset == Source) fanlight.SetEditorLayoutPreview(_runtimeLayout, changedBlockIndex);
            }

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private bool IsBlockCurrent(int blockIndex)
        {
            var artifact = Source.ActiveBake;

            if (!IsEmbeddedBake(Source, artifact) || artifact.BlockCount != Source.TotalBlockCount) return false;

            var baked = artifact.GetBlock(blockIndex);

            return baked.contentHash == _compiled.Blocks[blockIndex].contentHash
                   && string.Equals(baked.blockId, Source.GetBlock(blockIndex).BlockId, StringComparison.Ordinal);
        }

        private FanlightLayoutBakeArtifact FindEmbeddedBake(string path)
        {
            FanlightLayoutBakeArtifact found = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not FanlightLayoutBakeArtifact artifact) continue;

                if (found != null)
                {
                    throw new InvalidOperationException("The Layout Asset contains multiple bake artifacts. Remove the extra artifact before baking.");
                }

                found = artifact;
            }

            return found;
        }

        internal static bool IsEmbeddedBake(FanlightLayoutAsset layout, FanlightLayoutBakeArtifact artifact)
        {
            if (layout == null || artifact == null || !AssetDatabase.IsSubAsset(artifact)) return false;

            return string.Equals(
                AssetDatabase.GetAssetPath(layout),
                AssetDatabase.GetAssetPath(artifact),
                StringComparison.Ordinal);
        }

        private void ConvertBlock(int blockIndex)
        {
            var block = _compiled.Blocks[blockIndex];
            var end = block.contiguousSeatStart + block.contiguousSeatCount;

            for (var i = block.contiguousSeatStart; i < end; i++)
            {
                var seat = _compiled.Seats[i];
                _gpuSeats[i] = new FanlightSeatData(seat.localPosition, seat.planePosition, seat.blockCoordinates, (uint)i);
                _stableSeatIds[i] = seat.stableSeatId;
            }

            _gpuBlocks[blockIndex] = new FanlightBakedBlockData(
                block.localBounds.center,
                block.localBounds.extents.magnitude,
                block.contiguousSeatStart,
                block.contiguousSeatCount);
        }

        private void RefreshRuntimeLayout()
        {
            var contentHash = ComputeLayoutHash();

            _compiled.SetSummary(_boundsTree.Root, contentHash);
            _runtimeLayout = new FanlightRuntimeLayout(
                Source.LayoutId.Value,
                contentHash,
                Source.SeatPerBlock,
                Source.SeatPitch,
                Source.BlockCount,
                _boundsTree.Root,
                _gpuSeats,
                _stableSeatIds,
                _gpuBlocks);
            if (Source.SetContentHash(contentHash)) EditorUtility.SetDirty(Source);
        }

        private ulong ComputeLayoutHash()
        {
            var hash = FanlightStableHash.Begin();
            hash = FanlightStableHash.Add(hash, Source.LayoutId.Value);
            hash = FanlightStableHash.Add(hash, _hashTree.Root);
            return FanlightStableHash.Finish(hash);
        }

        private Vector3[] BuildCorners(int blockIndex)
        {
            var corners = new Vector3[4];

            WriteCorners(blockIndex, corners);

            return corners;
        }

        private void WriteCorners(int blockIndex, Vector3[] corners)
        {
            var block = Source.GetBlockCoordinates(blockIndex);
            var min = Source.GetPositionOnPlane(block, new int2(0, 0)) - Source.SeatPitch * 0.5f;
            var max = Source.GetPositionOnPlane(block, Source.SeatPerBlock - new int2(1, 1)) + Source.SeatPitch * 0.5f;
            corners[0] = Source.TransformBlockPoint(blockIndex, new Vector3(min.x, 0f, min.y));
            corners[1] = Source.TransformBlockPoint(blockIndex, new Vector3(max.x, 0f, min.y));
            corners[2] = Source.TransformBlockPoint(blockIndex, new Vector3(max.x, 0f, max.y));
            corners[3] = Source.TransformBlockPoint(blockIndex, new Vector3(min.x, 0f, max.y));
        }
    }
}
