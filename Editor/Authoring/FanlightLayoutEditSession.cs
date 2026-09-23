using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Rendering;
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
        private static bool _previewRefreshQueued;

        private readonly FanlightCompiledLayout _compiled;
        private readonly FanlightSeatData[] _gpuSeats;
        private readonly ulong[] _stableSeatIds;
        private readonly FanlightBakedBlockData[] _gpuBlocks;
        private readonly FanlightBoundsTree _boundsTree;
        private readonly Vector3[][] _corners;
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
            AssemblyReloadEvents.beforeAssemblyReload += ClearAll;
            EditorApplication.hierarchyChanged += QueuePreviewRefresh;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            QueuePreviewRefresh();
        }

        private FanlightLayoutEditSession(FanlightLayoutAsset source)
        {
            Source = source;
            _compiled = new FanlightCompiledLayout(source);
            _gpuSeats = new FanlightSeatData[source.TotalSeatCount];
            _stableSeatIds = new ulong[source.TotalSeatCount];
            _gpuBlocks = new FanlightBakedBlockData[source.BlockCount];
            _boundsTree = new FanlightBoundsTree(source.BlockCount);
            _corners = new Vector3[source.BlockCount][];
            RefreshCompiledData();
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
            ClearAll();
            QueuePreviewRefresh();
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

            QueuePreviewRefresh();
        }

        internal static bool ApplyTopologyChange(
            FanlightLayoutAsset source,
            string undoName,
            Func<bool> mutation)
        {
            if (Application.isPlaying || source == null || mutation == null) return false;

            Undo.RecordObject(source, undoName);
            if (!mutation()) return false;

            EditorUtility.SetDirty(source);
            Reset(source);
            FanlightLayoutIdRegistry.Invalidate();

            var session = Get(source);
            session?.ApplyPreviewToAllInstances(-1);
            return session != null;
        }

        internal Vector3[] GetCorners(int blockIndex) => _corners[blockIndex];

        internal Bounds GetBlockBounds(int blockIndex) => _compiled.Blocks[blockIndex].localBounds;

        internal void QueryVisible(Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            _boundsTree.Query(planes, localToWorld, results);
        }

        internal bool SetBlockPlacements(
            IReadOnlyList<int> blockIndices,
            IReadOnlyList<FanlightBlockPlacement> placements,
            string undoName)
        {
            if (Application.isPlaying
                || blockIndices == null
                || placements == null
                || blockIndices.Count == 0
                || blockIndices.Count != placements.Count)
            {
                return false;
            }

            Undo.RecordObject(Source, undoName);
            var changed = false;
            for (var i = 0; i < blockIndices.Count; i++)
            {
                changed |= Source.SetBlockPlacement(blockIndices[i], placements[i]);
            }

            if (!changed) return false;

            CommitGeometryChange();
            return true;
        }

        internal bool SetBlockRows(
            int blockIndex,
            FanlightLayoutRow[] rows,
            string undoName)
        {
            if (Application.isPlaying) return false;

            Undo.RecordObject(Source, undoName);
            if (!Source.SetBlockRows(blockIndex, rows)) return false;

            CommitGeometryChange();
            return true;
        }

        internal bool SetBlockRows(
            IReadOnlyList<int> blockIndices,
            IReadOnlyList<FanlightLayoutRow[]> rowSets,
            string undoName)
        {
            if (Application.isPlaying
                || blockIndices == null
                || rowSets == null
                || blockIndices.Count == 0
                || blockIndices.Count != rowSets.Count)
            {
                return false;
            }

            Undo.RecordObject(Source, undoName);
            var changed = false;
            for (var i = 0; i < blockIndices.Count; i++)
            {
                changed |= Source.SetBlockRows(blockIndices[i], rowSets[i]);
            }

            if (!changed) return false;

            CommitGeometryChange();
            return true;
        }

        internal bool Bake()
        {
            if (FanlightLayoutIdRegistry.IsDuplicate(Source))
            {
                EditorUtility.DisplayDialog("Invalid Fanlight Layout", "Duplicate layout ID detected. Baking is disabled.", "OK");
                return false;
            }

            _compiled.SetSummary(_runtimeLayout.LocalBounds, _runtimeLayout.ContentHash);

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
                    Undo.RegisterCreatedObjectUndo(artifact, "Create Fanlight Layout Bake");
                    AssetDatabase.AddObjectToAsset(artifact, Source);
                }

                artifact.name = $"{Source.name} Bake Artifact";
                artifact.hideFlags = HideFlags.NotEditable;
                artifact.Initialize(
                    Source.LayoutId.Value,
                    _compiled.ContentHash,
                    new Vector2(Source.ReferenceSeatSpacing.x, Source.ReferenceSeatSpacing.y),
                    _compiled.LocalBounds,
                    _compiled.Seats,
                    _compiled.Blocks);
                Source.SetActiveBake(artifact);

                EditorUtility.SetDirty(artifact);
                EditorUtility.SetDirty(Source);
                AssetDatabase.SaveAssets();
                EditorApplication.RepaintProjectWindow();

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

        internal static bool IsEmbeddedBake(FanlightLayoutAsset layout, FanlightLayoutBakeArtifact artifact)
        {
            if (layout == null || artifact == null || !AssetDatabase.IsSubAsset(artifact)) return false;

            return string.Equals(
                AssetDatabase.GetAssetPath(layout),
                AssetDatabase.GetAssetPath(artifact),
                StringComparison.Ordinal);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                QueuePreviewRefresh();
            }
        }

        private static void ClearAll()
        {
            Sessions.Clear();

            if (Application.isPlaying) return;

            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                fanlight.ClearEditorLayoutPreview();
            }
        }

        private static void QueuePreviewRefresh()
        {
            if (_previewRefreshQueued) return;

            _previewRefreshQueued = true;
            EditorApplication.delayCall += RefreshAllPreviews;
        }

        private static void RefreshAllPreviews()
        {
            _previewRefreshQueued = false;

            foreach (var fanlight in Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                var layout = fanlight.LayoutAsset;
                if (layout == null || !layout.IsInitialized)
                {
                    fanlight.ClearEditorLayoutPreview();
                    continue;
                }

                if (FanlightLayoutIdRegistry.IsDuplicate(layout))
                {
                    fanlight.SetEditorLayoutBlocked(true);
                    continue;
                }

                var session = Get(layout);
                if (session == null) continue;

                fanlight.SetEditorLayoutBlocked(false);
                if (fanlight.EditorPreviewContentHash != session.RuntimeLayout.ContentHash)
                {
                    fanlight.SetEditorLayoutPreview(session.RuntimeLayout, -1);
                }
            }

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private void CommitGeometryChange()
        {
            EditorUtility.SetDirty(Source);
            RefreshCompiledData();
            ApplyPreviewToAllInstances(-1);
        }

        private void RefreshCompiledData()
        {
            _compiled.CompileAll();

            for (var blockIndex = 0; blockIndex < Source.BlockCount; blockIndex++)
            {
                ConvertBlock(blockIndex);
                _boundsTree.Update(blockIndex, _compiled.Blocks[blockIndex].localBounds);
                _corners[blockIndex] = BuildCorners(blockIndex);
            }

            _dirtyBlockCount = 0;
            for (var blockIndex = 0; blockIndex < Source.BlockCount; blockIndex++)
            {
                if (!IsBlockCurrent(blockIndex)) _dirtyBlockCount++;
            }

            RefreshRuntimeLayout();
        }

        private bool IsBlockCurrent(int blockIndex)
        {
            var artifact = Source.ActiveBake;
            if (!IsEmbeddedBake(Source, artifact) || artifact.BlockCount != Source.BlockCount) return false;

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

        private void ConvertBlock(int blockIndex)
        {
            var block = _compiled.Blocks[blockIndex];
            var end = block.contiguousSeatStart + block.contiguousSeatCount;

            for (var i = block.contiguousSeatStart; i < end; i++)
            {
                var seat = _compiled.Seats[i];
                _gpuSeats[i] = new FanlightSeatData(seat.localPosition, seat.blockIndex, (uint)i);
                _stableSeatIds[i] = seat.stableSeatId;
            }

            _gpuBlocks[blockIndex] = new FanlightBakedBlockData(
                block.localBounds.center,
                block.localBounds.extents.magnitude,
                block.contiguousSeatStart,
                block.contiguousSeatCount,
                block.effectCoordinate);
        }

        private void RefreshRuntimeLayout()
        {
            _runtimeLayout = new FanlightRuntimeLayout(
                Source.LayoutId.Value,
                _compiled.ContentHash,
                Source.ReferenceSeatSpacing,
                _compiled.LocalBounds,
                _gpuSeats,
                _stableSeatIds,
                BuildStableBlockIds(),
                _gpuBlocks);

            if (Source.SetContentHash(_compiled.ContentHash)) EditorUtility.SetDirty(Source);
        }

        private string[] BuildStableBlockIds()
        {
            var blockIds = new string[Source.BlockCount];
            for (var i = 0; i < blockIds.Length; i++) blockIds[i] = Source.GetBlock(i).BlockId;
            return blockIds;
        }

        private Vector3[] BuildCorners(int blockIndex)
        {
            var block = Source.GetBlock(blockIndex);
            var first = block.GetRow(0);
            var last = block.GetRow(block.RowCount - 1);
            var placement = block.Placement;
            var rotation = placement.Rotation;

            if (block.RowCount == 1)
            {
                var halfDepth = Mathf.Max(0.1f, Vector3.Distance(first.LeftPoint, first.RightPoint) * 0.05f);
                return new[]
                {
                    placement.position + rotation * (first.LeftPoint - Vector3.forward * halfDepth),
                    placement.position + rotation * (first.RightPoint - Vector3.forward * halfDepth),
                    placement.position + rotation * (first.RightPoint + Vector3.forward * halfDepth),
                    placement.position + rotation * (first.LeftPoint + Vector3.forward * halfDepth)
                };
            }

            return new[]
            {
                placement.position + rotation * first.LeftPoint,
                placement.position + rotation * first.RightPoint,
                placement.position + rotation * last.RightPoint,
                placement.position + rotation * last.LeftPoint
            };
        }
    }
}
