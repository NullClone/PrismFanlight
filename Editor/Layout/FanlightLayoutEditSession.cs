using System;
using System.Collections;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Rendering;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [InitializeOnLoad]
    internal sealed class FanlightLayoutEditSession
    {
        private static readonly Dictionary<int, FanlightLayoutEditSession> Sessions = new();

        private readonly FanlightCompiledLayout _compiled;
        private readonly FanlightSeatData[] _gpuSeats;
        private readonly FanlightBakedBlockData[] _gpuBlocks;
        private readonly FanlightBoundsTree _boundsTree;
        private readonly FanlightHashTree _hashTree;
        private readonly Vector3[][] _corners;
        private readonly BitArray _dirtyBlocks;
        private int _dirtyBlockCount;
        private FanlightLayoutDirtyReason _dirtyReason;
        private int _knownLayoutVersion;
        private FanlightRuntimeLayout _runtimeLayout;

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

            _knownLayoutVersion = source.LayoutVersion;
            _dirtyReason = source.ActiveBake == null
                ? FanlightLayoutDirtyReason.Topology | FanlightLayoutDirtyReason.BakeSchema
                : _dirtyBlockCount > 0
                    ? FanlightLayoutDirtyReason.BlockPlacement
                    : FanlightLayoutDirtyReason.None;
            RefreshRuntimeLayout();
        }

        public FanlightLayoutAsset Source { get; }

        public FanlightRuntimeLayout RuntimeLayout => _runtimeLayout;

        public int DirtyBlockCount => _dirtyBlockCount;

        public long EstimatedDirtySeatBytes => (long)DirtyBlockCount * Source.BlockSeatCount * FanlightSeatData.Stride;

        public FanlightLayoutDirtyReason DirtyReason => _dirtyReason;

        public static FanlightLayoutEditSession Get(FanlightLayoutAsset source)
        {
            if (source == null || !source.IsInitialized) return null;
            var key = source.GetInstanceID();
            if (!Sessions.TryGetValue(key, out var session)
                || session.Source != source
                || session._knownLayoutVersion != source.LayoutVersion)
            {
                session = new FanlightLayoutEditSession(source);
                Sessions[key] = session;
            }
            return session;
        }

        public static void ResetAll()
        {
            Sessions.Clear();
            if (Application.isPlaying) return;
            foreach (var fanlight in UnityEngine.Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                fanlight.ClearEditorLayoutPreview();
            }
        }

        public Vector3[] GetCorners(int blockIndex) => _corners[blockIndex];

        public Bounds GetBlockBounds(int blockIndex) => _compiled.Blocks[blockIndex].localBounds;

        public void QueryVisible(Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            _boundsTree.Query(planes, localToWorld, results);
        }

        public bool SetBlockPlacement(int blockIndex, FanlightBlockPlacement placement, string undoName)
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
            _dirtyReason |= FanlightLayoutDirtyReason.BlockPlacement;
            _knownLayoutVersion = Source.LayoutVersion;
            RefreshRuntimeLayout();
            ApplyPreviewToAllInstances(blockIndex);
            return true;
        }

        public bool BakeWithSaveDialog()
        {
            if (FanlightLayoutIdRegistry.IsDuplicate(Source))
            {
                EditorUtility.DisplayDialog("Invalid Fanlight Layout", "Duplicate layout ID detected. Baking is disabled.", "OK");
                return false;
            }

            _compiled.SetSummary(_boundsTree.Root, ComputeLayoutHash());
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Fanlight Layout Bake",
                Source.name,
                "pflayoutbake",
                "Choose where to save this immutable layout bake artifact.");
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                FanlightLayoutBakeFile.Write(path, _compiled);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var artifact = AssetDatabase.LoadAssetAtPath<FanlightLayoutBakeArtifact>(path);
                if (artifact == null) throw new InvalidOperationException("The imported layout bake artifact is unavailable.");

                Undo.RecordObject(Source, "Assign Fanlight Layout Bake");
                Source.SetActiveBake(artifact);
                EditorUtility.SetDirty(Source);
                AssetDatabase.SaveAssets();
                for (var i = 0; i < _dirtyBlocks.Length; i++) _dirtyBlocks[i] = false;
                _dirtyBlockCount = 0;
                _dirtyReason = FanlightLayoutDirtyReason.None;
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

        public void ApplyPreviewToAllInstances(int changedBlockIndex)
        {
            foreach (var fanlight in UnityEngine.Object.FindObjectsByType<PrismFanlight>(FindObjectsSortMode.None))
            {
                if (fanlight.LayoutAsset == Source) fanlight.SetEditorLayoutPreview(_runtimeLayout, changedBlockIndex);
            }
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private bool IsBlockCurrent(int blockIndex)
        {
            var artifact = Source.ActiveBake;
            if (artifact == null || artifact.BlockCount != Source.TotalBlockCount) return false;
            var baked = artifact.GetBlock(blockIndex);
            var source = Source.GetBlock(blockIndex);
            return baked.sourceRevision == source.AuthoringRevision
                   && string.Equals(baked.blockId, source.BlockId, StringComparison.Ordinal);
        }

        private void ConvertBlock(int blockIndex)
        {
            var block = _compiled.Blocks[blockIndex];
            var end = block.contiguousSeatStart + block.contiguousSeatCount;
            for (var i = block.contiguousSeatStart; i < end; i++)
            {
                var seat = _compiled.Seats[i];
                _gpuSeats[i] = new FanlightSeatData(seat.localPosition, seat.planePosition, seat.blockCoordinates, (uint)i);
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
                Source.LayoutVersion,
                FanlightLayoutBakeArtifact.CurrentFormatVersion,
                contentHash,
                Source.SeatPerBlock,
                Source.SeatPitch,
                Source.BlockCount,
                _boundsTree.Root,
                _gpuSeats,
                _gpuBlocks);
        }

        private ulong ComputeLayoutHash()
        {
            var hash = FanlightStableHash.Begin();
            hash = FanlightStableHash.Add(hash, Source.LayoutId.Value);
            hash = FanlightStableHash.Add(hash, Source.LayoutVersion);
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
            var min = Source.GetPositionOnPlane(block, new Unity.Mathematics.int2(0, 0)) - Source.SeatPitch * 0.5f;
            var max = Source.GetPositionOnPlane(block, Source.SeatPerBlock - new Unity.Mathematics.int2(1, 1)) + Source.SeatPitch * 0.5f;
            corners[0] = Source.TransformBlockPoint(blockIndex, new Vector3(min.x, 0f, min.y));
            corners[1] = Source.TransformBlockPoint(blockIndex, new Vector3(max.x, 0f, min.y));
            corners[2] = Source.TransformBlockPoint(blockIndex, new Vector3(max.x, 0f, max.y));
            corners[3] = Source.TransformBlockPoint(blockIndex, new Vector3(min.x, 0f, max.y));
        }
    }

    internal sealed class FanlightHashTree
    {
        private readonly int _size;
        private readonly ulong[] _nodes;

        public FanlightHashTree(int count)
        {
            _size = 1;
            while (_size < count) _size <<= 1;
            _nodes = new ulong[_size * 2];
        }

        public ulong Root => _nodes[1];

        public void Update(int index, ulong value)
        {
            var node = _size + index;
            _nodes[node] = value;
            while ((node >>= 1) > 0)
            {
                var hash = FanlightStableHash.Begin();
                hash = FanlightStableHash.Add(hash, _nodes[node * 2]);
                hash = FanlightStableHash.Add(hash, _nodes[node * 2 + 1]);
                _nodes[node] = FanlightStableHash.Finish(hash);
            }
        }
    }

    internal sealed class FanlightBoundsTree
    {
        private readonly int _count;
        private readonly int _size;
        private readonly Bounds[] _nodes;
        private readonly bool[] _valid;

        public FanlightBoundsTree(int count)
        {
            _count = count;
            _size = 1;
            while (_size < count) _size <<= 1;
            _nodes = new Bounds[_size * 2];
            _valid = new bool[_size * 2];
        }

        public Bounds Root => _valid[1] ? _nodes[1] : new Bounds(Vector3.zero, Vector3.one);

        public void Update(int index, Bounds bounds)
        {
            var node = _size + index;
            _nodes[node] = bounds;
            _valid[node] = true;
            while ((node >>= 1) > 0) Rebuild(node);
        }

        public void Query(Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            results.Clear();
            QueryNode(1, planes, localToWorld, results);
        }

        private void QueryNode(int node, Plane[] planes, Matrix4x4 localToWorld, List<int> results)
        {
            if (node >= _nodes.Length || !_valid[node]) return;
            var worldBounds = FanlightGeometryBuilder.TransformBounds(localToWorld, _nodes[node]);
            if (!GeometryUtility.TestPlanesAABB(planes, worldBounds)) return;
            if (node >= _size)
            {
                var index = node - _size;
                if (index < _count) results.Add(index);
                return;
            }
            QueryNode(node * 2, planes, localToWorld, results);
            QueryNode(node * 2 + 1, planes, localToWorld, results);
        }

        private void Rebuild(int node)
        {
            var left = node * 2;
            var right = left + 1;
            if (!_valid[left] && !_valid[right])
            {
                _valid[node] = false;
                return;
            }
            if (!_valid[right])
            {
                _nodes[node] = _nodes[left];
                _valid[node] = true;
                return;
            }
            if (!_valid[left])
            {
                _nodes[node] = _nodes[right];
                _valid[node] = true;
                return;
            }
            var bounds = _nodes[left];
            bounds.Encapsulate(_nodes[right].min);
            bounds.Encapsulate(_nodes[right].max);
            _nodes[node] = bounds;
            _valid[node] = true;
        }
    }
}
