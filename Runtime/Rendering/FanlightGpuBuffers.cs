using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuBuffers
    {
        // Fields

        private const int PaletteSlotCount = 6;

        private readonly FanlightBlockData[] _singleBlockUpload = new FanlightBlockData[1];
        private readonly FanlightMotionSample[] _motionSamples = new FanlightMotionSample[FanlightMotionAsset.SampleCount];
        private readonly FanlightMotionSample[] _motionSourceSamples = new FanlightMotionSample[FanlightMotionAsset.SampleCount * 3];
        private readonly FanlightMotionAsset[] _motionAssets = new FanlightMotionAsset[3];
        private readonly int[] _motionRevisions = new int[3];
        private readonly bool[] _runtimeBlockPaletteUploaded = new bool[3];
        private uint[] _runtimeBlockPaletteSlots = Array.Empty<uint>();
        private uint[] _runtimeBlockPaletteCandidate = Array.Empty<uint>();
        private bool[] _runtimeBlockPaletteAssigned = Array.Empty<bool>();
        private readonly bool[] _runtimeBlockPulseGroupsUploaded = new bool[3];
        private uint[] _runtimeBlockPulseGroups = Array.Empty<uint>();
        private uint[] _runtimeBlockPulseGroupCandidate = Array.Empty<uint>();
        private bool[] _runtimeBlockPulseGroupAssigned = Array.Empty<bool>();
        private FanlightMotionSample _motionReferencePose;
        private Vector3 _motionWeights;
        private bool _hasMotionData;


        // Properties

        internal ComputeBuffer SeatBuffer { get; private set; }

        internal ComputeBuffer BlockBuffer { get; private set; }

        internal ComputeBuffer BlockVisibilityBuffer { get; private set; }

        internal ComputeBuffer PenlightVisibleIndexBuffer { get; private set; }

        internal ComputeBuffer PenlightVariantAssignmentBuffer { get; private set; }

        internal ComputeBuffer PenlightVariantOffsetBuffer { get; private set; }

        internal ComputeBuffer AudienceVisibleIndexBuffer { get; private set; }

        internal ComputeBuffer MatrixBuffer { get; private set; }

        internal ComputeBuffer StableAssignmentBuffer { get; private set; }

        internal ComputeBuffer ResolvedChromaBuffer { get; private set; }

        internal ComputeBuffer ResolvedMaskBuffer { get; private set; }

        internal ComputeBuffer RuntimeBlockPaletteBuffer { get; private set; }

        internal ComputeBuffer RuntimeBlockPulseGroupBuffer { get; private set; }

        internal ComputeBuffer RandomBuffer { get; private set; }

        internal ComputeBuffer MotionSampleBuffer { get; private set; }

        internal GraphicsBuffer PenlightArgsBuffer { get; private set; }

        internal ComputeBuffer AudiencePartBuffer { get; private set; }

        internal GraphicsBuffer AudienceArgsBuffer { get; private set; }

        internal bool HasAudience => AudiencePartBuffer != null;

        internal int SeatCount { get; private set; }

        internal int BlockCount { get; private set; }

        internal Bounds LocalBounds { get; private set; }

        internal int PenlightVariantCount { get; private set; }

        internal uint[] PenlightVariantOffsets { get; private set; } = Array.Empty<uint>();

        internal Vector4 PenlightVariantGripPivotYs { get; private set; }

        internal Vector4 MotionReferenceArm => _motionReferencePose.ArmDirectionExtension;

        internal Vector4 MotionReferencePenlight => _motionReferencePose.PenlightDirectionBodyLean;


        // Methods

        internal void Allocate(
            FanlightPenlightRuntimeAppearance appearance,
            FanlightRuntimeLayout layout,
            bool allocateAudience,
            Mesh audienceMesh,
            uint globalSeed)
        {
            Release();

            SeatCount = layout.SeatCount;
            BlockCount = layout.BlockCount;
            PenlightVariantCount = appearance.VariantCount;
            LocalBounds = ExpandBounds(layout.LocalBounds, appearance.BoundsPadding);
            PenlightVariantGripPivotYs = BuildGripPivotVector(appearance.GripPivotYs);
            _runtimeBlockPaletteSlots = new uint[BlockCount * 3];
            _runtimeBlockPaletteCandidate = new uint[BlockCount];
            _runtimeBlockPaletteAssigned = new bool[BlockCount];
            Array.Clear(_runtimeBlockPaletteUploaded, 0, _runtimeBlockPaletteUploaded.Length);
            _runtimeBlockPulseGroups = new uint[BlockCount * 3];
            _runtimeBlockPulseGroupCandidate = new uint[BlockCount];
            _runtimeBlockPulseGroupAssigned = new bool[BlockCount];
            Array.Clear(_runtimeBlockPulseGroupsUploaded, 0, _runtimeBlockPulseGroupsUploaded.Length);

            var assignments = BuildVariantAssignments(layout, appearance, out var counts);
            PenlightVariantOffsets = BuildVariantOffsets(counts);

            SeatBuffer = new ComputeBuffer(SeatCount, FanlightSeatData.Stride, ComputeBufferType.Structured);
            BlockBuffer = new ComputeBuffer(BlockCount, FanlightBlockData.Stride, ComputeBufferType.Structured);
            BlockVisibilityBuffer = new ComputeBuffer(BlockCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVariantAssignmentBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            PenlightVariantOffsetBuffer = new ComputeBuffer(PenlightVariantCount, sizeof(uint), ComputeBufferType.Structured);
            AudienceVisibleIndexBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            MatrixBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 16, ComputeBufferType.Structured);
            StableAssignmentBuffer = new ComputeBuffer(SeatCount, sizeof(uint), ComputeBufferType.Structured);
            ResolvedChromaBuffer = new ComputeBuffer(SeatCount, sizeof(float) * 4, ComputeBufferType.Structured);
            ResolvedMaskBuffer = new ComputeBuffer(SeatCount, sizeof(float), ComputeBufferType.Structured);
            RuntimeBlockPaletteBuffer = new ComputeBuffer(BlockCount * 3, sizeof(uint), ComputeBufferType.Structured);
            RuntimeBlockPulseGroupBuffer = new ComputeBuffer(BlockCount * 3, sizeof(uint), ComputeBufferType.Structured);
            RandomBuffer = new ComputeBuffer(SeatCount, FanlightRandomData.Stride, ComputeBufferType.Structured);
            MotionSampleBuffer = new ComputeBuffer(_motionSamples.Length, FanlightMotionSample.Stride, ComputeBufferType.Structured);
            PenlightArgsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                PenlightVariantCount,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
            AudienceArgsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);

            SeatBuffer.SetData(layout.Seats);
            BlockBuffer.SetData(BuildBlockData(layout, appearance.BoundsPadding));
            PenlightVariantAssignmentBuffer.SetData(assignments);
            PenlightVariantOffsetBuffer.SetData(PenlightVariantOffsets);
            UpdateRandomData(globalSeed, layout);
            ResetPenlightArgs(PenlightArgsBuffer, appearance.Meshes);
            ResetArgs(AudienceArgsBuffer, audienceMesh);

            if (allocateAudience)
            {
                AudiencePartBuffer = new ComputeBuffer(SeatCount * FanlightAudiencePart.PartsPerSeat, FanlightAudiencePart.Stride, ComputeBufferType.Structured);
            }
        }

        internal void UpdateStaticData(FanlightPenlightRuntimeAppearance appearance, FanlightRuntimeLayout layout)
        {
            if (SeatBuffer == null || BlockBuffer == null || layout.SeatCount != SeatCount || layout.BlockCount != BlockCount)
            {
                throw new InvalidOperationException("Static layout topology does not match allocated GPU buffers.");
            }

            SeatBuffer.SetData(layout.Seats);
            BlockBuffer.SetData(BuildBlockData(layout, appearance.BoundsPadding));
            LocalBounds = ExpandBounds(layout.LocalBounds, appearance.BoundsPadding);
            Array.Clear(_runtimeBlockPaletteUploaded, 0, _runtimeBlockPaletteUploaded.Length);
            Array.Clear(_runtimeBlockPulseGroupsUploaded, 0, _runtimeBlockPulseGroupsUploaded.Length);
        }

        internal void UpdateBlock(FanlightPenlightRuntimeAppearance appearance, FanlightRuntimeLayout layout, int blockIndex)
        {
            if (SeatBuffer == null || BlockBuffer == null || blockIndex < 0 || blockIndex >= layout.BlockCount) return;

            var block = layout.Blocks[blockIndex];
            if (block.count > 0)
            {
                SeatBuffer.SetData(layout.Seats, block.startIndex, block.startIndex, block.count);
            }

            _singleBlockUpload[0] = ToBlockData(block, appearance.BoundsPadding);
            BlockBuffer.SetData(_singleBlockUpload, 0, blockIndex, 1);
            LocalBounds = ExpandBounds(layout.LocalBounds, appearance.BoundsPadding);
        }

        private static FanlightBlockData[] BuildBlockData(FanlightRuntimeLayout layout, float boundsPadding)
        {
            var data = new FanlightBlockData[layout.BlockCount];
            for (var i = 0; i < data.Length; i++) data[i] = ToBlockData(layout.Blocks[i], boundsPadding);
            return data;
        }

        private static FanlightBlockData ToBlockData(FanlightBakedBlockData block, float boundsPadding)
        {
            return new FanlightBlockData(
                block.localCenter,
                block.radius + boundsPadding,
                block.startIndex,
                block.count,
                block.effectCoordinate);
        }

        private static Bounds ExpandBounds(Bounds bounds, float boundsPadding)
        {
            bounds.Expand(boundsPadding * 2f);
            return bounds;
        }

        internal void UpdateRandomData(uint globalSeed, FanlightRuntimeLayout layout)
        {
            if (RandomBuffer == null
                || StableAssignmentBuffer == null
                || layout == null
                || layout.SeatCount != SeatCount
                || SeatCount <= 0)
            {
                return;
            }

            RandomBuffer.SetData(BuildRandomData(layout, globalSeed));
            StableAssignmentBuffer.SetData(BuildStableAssignments(layout, globalSeed));
        }

        internal void UpdateRuntimeBlockPaletteData(FanlightColorState color, FanlightRuntimeLayout layout)
        {
            if (RuntimeBlockPaletteBuffer == null
                || layout == null
                || layout.BlockCount != BlockCount)
            {
                throw new InvalidOperationException("Runtime Block Palette Buffer is not available.");
            }

            for (var sourceIndex = 0; sourceIndex < 3; sourceIndex++)
            {
                if (color.GetSourceWeight(sourceIndex) <= 0f) continue;
                var source = color.GetSource(sourceIndex);
                if (source.Mode != FanlightColorMode.BlockPalette) continue;

                Array.Clear(_runtimeBlockPaletteCandidate, 0, _runtimeBlockPaletteCandidate.Length);
                Array.Clear(_runtimeBlockPaletteAssigned, 0, _runtimeBlockPaletteAssigned.Length);
                for (var entryIndex = 0; entryIndex < source.BlockPaletteEntryCount; entryIndex++)
                {
                    var entry = source.GetBlockPaletteEntry(entryIndex);
                    var blockIndex = layout.GetBlockIndex(entry.StableBlockId);
                    if (blockIndex < 0)
                    {
                        throw new InvalidOperationException("Block Palette contains an unknown Stable Block ID.");
                    }

                    if (_runtimeBlockPaletteAssigned[blockIndex])
                    {
                        throw new InvalidOperationException("Block Palette contains a duplicate Stable Block ID.");
                    }

                    _runtimeBlockPaletteAssigned[blockIndex] = true;
                    _runtimeBlockPaletteCandidate[blockIndex] = (uint)entry.PaletteSlot;
                }

                var laneStart = sourceIndex * BlockCount;
                var changed = !_runtimeBlockPaletteUploaded[sourceIndex];

                for (var blockIndex = 0; blockIndex < _runtimeBlockPaletteAssigned.Length; blockIndex++)
                {
                    if (!_runtimeBlockPaletteAssigned[blockIndex])
                    {
                        throw new InvalidOperationException("Block Palette must specify every Block in the active Layout.");
                    }

                    if (_runtimeBlockPaletteSlots[laneStart + blockIndex] != _runtimeBlockPaletteCandidate[blockIndex])
                    {
                        changed = true;
                    }
                }

                if (!changed) continue;

                Array.Copy(
                    _runtimeBlockPaletteCandidate,
                    0,
                    _runtimeBlockPaletteSlots,
                    laneStart,
                    BlockCount);
                RuntimeBlockPaletteBuffer.SetData(
                    _runtimeBlockPaletteSlots,
                    laneStart,
                    laneStart,
                    BlockCount);
                _runtimeBlockPaletteUploaded[sourceIndex] = true;
            }
        }

        internal void UpdateRuntimeBlockPulseGroupData(FanlightIntensityState intensity, FanlightRuntimeLayout layout)
        {
            if (RuntimeBlockPulseGroupBuffer == null
                || layout == null
                || layout.BlockCount != BlockCount)
            {
                throw new InvalidOperationException("Runtime Block Pulse Group Buffer is not available.");
            }

            for (var sourceIndex = 0; sourceIndex < 3; sourceIndex++)
            {
                if (intensity.GetMaskWeight(sourceIndex) <= 0f) continue;
                var mask = intensity.GetMask(sourceIndex);
                if (mask.Mode != FanlightIntensityMaskMode.BlockAlternatingPulse) continue;

                Array.Clear(_runtimeBlockPulseGroupCandidate, 0, _runtimeBlockPulseGroupCandidate.Length);
                Array.Clear(_runtimeBlockPulseGroupAssigned, 0, _runtimeBlockPulseGroupAssigned.Length);
                for (var entryIndex = 0; entryIndex < mask.BlockPulseEntryCount; entryIndex++)
                {
                    var entry = mask.GetBlockPulseEntry(entryIndex);
                    var blockIndex = layout.GetBlockIndex(entry.StableBlockId);
                    if (blockIndex < 0)
                    {
                        throw new InvalidOperationException(
                            "Block Alternating Pulse contains an unknown Stable Block ID.");
                    }

                    if (_runtimeBlockPulseGroupAssigned[blockIndex])
                    {
                        throw new InvalidOperationException(
                            "Block Alternating Pulse contains a duplicate Stable Block ID.");
                    }

                    _runtimeBlockPulseGroupAssigned[blockIndex] = true;
                    _runtimeBlockPulseGroupCandidate[blockIndex] = (uint)entry.Group;
                }

                var laneStart = sourceIndex * BlockCount;
                var changed = !_runtimeBlockPulseGroupsUploaded[sourceIndex];

                for (var blockIndex = 0; blockIndex < _runtimeBlockPulseGroupAssigned.Length; blockIndex++)
                {
                    if (!_runtimeBlockPulseGroupAssigned[blockIndex])
                    {
                        throw new InvalidOperationException(
                            "Block Alternating Pulse must specify every Block in the active Layout.");
                    }

                    if (_runtimeBlockPulseGroups[laneStart + blockIndex]
                        != _runtimeBlockPulseGroupCandidate[blockIndex])
                    {
                        changed = true;
                    }
                }

                if (!changed) continue;

                Array.Copy(
                    _runtimeBlockPulseGroupCandidate,
                    0,
                    _runtimeBlockPulseGroups,
                    laneStart,
                    BlockCount);
                RuntimeBlockPulseGroupBuffer.SetData(
                    _runtimeBlockPulseGroups,
                    laneStart,
                    laneStart,
                    BlockCount);
                _runtimeBlockPulseGroupsUploaded[sourceIndex] = true;
            }
        }

        internal bool HasMotionAssetChanges(FanlightMotionState motion)
        {
            if (MotionSampleBuffer == null) throw new InvalidOperationException("Motion sample buffer is not allocated.");

            if (!_hasMotionData) return true;

            for (var i = 0; i < 3; i++)
            {
                var asset = motion.GetAsset(i);
                var revision = asset != null ? asset.BakeRevision : 0;
                if (_motionAssets[i] != asset || _motionRevisions[i] != revision) return true;
            }

            return false;
        }

        internal void UpdateMotionData(FanlightMotionState motion)
        {
            if (MotionSampleBuffer == null) throw new InvalidOperationException("Motion sample buffer is not allocated.");

            var weights = new Vector3(
                motion.GetAssetWeight(0),
                motion.GetAssetWeight(1),
                motion.GetAssetWeight(2));
            var assetsChanged = !_hasMotionData;

            for (var i = 0; i < 3; i++)
            {
                var asset = motion.GetAsset(i);
                if (weights[i] > 0f && (asset == null || !asset.HasValidBake))
                {
                    throw new InvalidOperationException("Motion state contains an invalid baked asset.");
                }

                var revision = asset != null ? asset.BakeRevision : 0;
                if (_motionAssets[i] == asset && _motionRevisions[i] == revision) continue;

                var destinationIndex = i * FanlightMotionAsset.SampleCount;
                if (asset != null && asset.HasValidBake)
                {
                    asset.CopyBakedSamples(_motionSourceSamples, destinationIndex);
                }
                else
                {
                    Array.Clear(_motionSourceSamples, destinationIndex, FanlightMotionAsset.SampleCount);
                }

                _motionAssets[i] = asset;
                _motionRevisions[i] = revision;
                assetsChanged = true;
            }

            if (!assetsChanged && _motionWeights.Equals(weights)) return;

            for (var sampleIndex = 0; sampleIndex < FanlightMotionAsset.SampleCount; sampleIndex++)
            {
                _motionSamples[sampleIndex] = BlendMotionSamples(
                    _motionSourceSamples[sampleIndex],
                    _motionSourceSamples[FanlightMotionAsset.SampleCount + sampleIndex],
                    _motionSourceSamples[FanlightMotionAsset.SampleCount * 2 + sampleIndex],
                    weights);
            }

            _motionReferencePose = BlendMotionSamples(
                _motionAssets[0] != null ? _motionAssets[0].ReferencePose : default,
                _motionAssets[1] != null ? _motionAssets[1].ReferencePose : default,
                _motionAssets[2] != null ? _motionAssets[2].ReferencePose : default,
                weights);
            MotionSampleBuffer.SetData(_motionSamples);
            _motionWeights = weights;
            _hasMotionData = true;
        }

        private static FanlightMotionSample BlendMotionSamples(
            FanlightMotionSample sampleA,
            FanlightMotionSample sampleB,
            FanlightMotionSample sampleC,
            Vector3 weights)
        {
            return new FanlightMotionSample(
                BlendDirections(
                    sampleA.ArmDirection,
                    sampleB.ArmDirection,
                    sampleC.ArmDirection,
                    weights,
                    Vector3.forward),
                sampleA.ArmExtension * weights.x
                + sampleB.ArmExtension * weights.y
                + sampleC.ArmExtension * weights.z,
                BlendDirections(
                    sampleA.PenlightDirection,
                    sampleB.PenlightDirection,
                    sampleC.PenlightDirection,
                    weights,
                    Vector3.up),
                sampleA.BodyLean * weights.x
                + sampleB.BodyLean * weights.y
                + sampleC.BodyLean * weights.z);
        }

        private static Vector3 BlendDirections(
            Vector3 directionA,
            Vector3 directionB,
            Vector3 directionC,
            Vector3 weights,
            Vector3 fallback)
        {
            var result = fallback;
            var totalWeight = 0f;
            BlendDirection(ref result, ref totalWeight, directionA, weights.x, fallback);
            BlendDirection(ref result, ref totalWeight, directionB, weights.y, fallback);
            BlendDirection(ref result, ref totalWeight, directionC, weights.z, fallback);
            return result;
        }

        private static void BlendDirection(
            ref Vector3 result,
            ref float totalWeight,
            Vector3 direction,
            float weight,
            Vector3 fallback)
        {
            if (weight <= 0f) return;

            direction = NormalizeDirection(direction, fallback);
            if (totalWeight <= 0f)
            {
                result = direction;
                totalWeight = weight;
                return;
            }

            var nextTotal = totalWeight + weight;
            result = InterpolateDirection(result, direction, weight / nextTotal);
            totalWeight = nextTotal;
        }

        private static Vector3 InterpolateDirection(Vector3 from, Vector3 to, float weight)
        {
            from = NormalizeDirection(from, Vector3.up);
            to = NormalizeDirection(to, from);
            weight = Mathf.Clamp01(weight);
            if (weight <= 0f) return from;
            if (weight >= 1f) return to;

            var cosine = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
            if (cosine >= 0.9995f) return NormalizeDirection(Vector3.Lerp(from, to, weight), from);

            if (cosine <= -0.999999f)
            {
                var axisAngle = Mathf.PI * weight;
                var axis = DirectionFallbackAxis(from);
                return NormalizeDirection(
                    from * Mathf.Cos(axisAngle) + Vector3.Cross(axis, from) * Mathf.Sin(axisAngle),
                    from);
            }

            var theta = Mathf.Acos(cosine);
            var inverseSinTheta = 1f / Mathf.Sin(theta);
            var fromWeight = Mathf.Sin((1f - weight) * theta) * inverseSinTheta;
            var toWeight = Mathf.Sin(weight * theta) * inverseSinTheta;
            return NormalizeDirection(from * fromWeight + to * toWeight, from);
        }

        private static Vector3 DirectionFallbackAxis(Vector3 direction)
        {
            var absolute = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            var reference = absolute.x <= absolute.y && absolute.x <= absolute.z
                ? Vector3.right
                : absolute.y <= absolute.z
                    ? Vector3.up
                    : Vector3.forward;
            return NormalizeDirection(Vector3.Cross(direction, reference), Vector3.right);
        }

        private static Vector3 NormalizeDirection(Vector3 direction, Vector3 fallback)
        {
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f) return fallback;
            return direction.normalized;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static void ResetArgs(GraphicsBuffer argsBuffer, Mesh mesh)
        {
            argsBuffer.SetData(new[]
            {
                new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = mesh != null ? mesh.GetIndexCount(0) : 0u,
                    instanceCount = 0u,
                    startIndex = mesh != null ? mesh.GetIndexStart(0) : 0u,
                    baseVertexIndex = mesh != null ? mesh.GetBaseVertex(0) : 0,
                    startInstance = 0u
                }
            });
        }

        private static void ResetPenlightArgs(GraphicsBuffer argsBuffer, Mesh[] meshes)
        {
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[meshes.Length];
            for (var i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                args[i] = new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = mesh.GetIndexCount(0),
                    instanceCount = 0u,
                    startIndex = mesh.GetIndexStart(0),
                    baseVertexIndex = mesh.GetBaseVertex(0),
                    startInstance = 0u
                };
            }

            argsBuffer.SetData(args);
        }

        private static uint[] BuildVariantAssignments(FanlightRuntimeLayout layout, FanlightPenlightRuntimeAppearance appearance, out int[] counts)
        {
            var assignments = new uint[layout.SeatCount];
            counts = new int[appearance.VariantCount];

            for (var i = 0; i < assignments.Length; i++)
            {
                var variantIndex = 0;
                var stableSeatId = layout.StableSeatIds[i];
                if (appearance.VariantCount > 1)
                {
                    variantIndex = FanlightPenlightAssignment.SelectVariantIndex(
                        stableSeatId,
                        appearance.AssignmentSeed,
                        FanlightPenlightAssignment.PersonaAlgorithmVersion,
                        appearance.StableVariantIds);
                }

                assignments[i] = (uint)variantIndex;
                counts[variantIndex]++;
            }

            return assignments;
        }

        private static uint[] BuildVariantOffsets(int[] counts)
        {
            var offsets = new uint[counts.Length];
            var offset = 0u;
            for (var i = 0; i < counts.Length; i++)
            {
                offsets[i] = offset;
                offset += (uint)counts[i];
            }

            return offsets;
        }

        private static Vector4 BuildGripPivotVector(float[] pivots)
        {
            return new Vector4(
                pivots.Length > 0 ? pivots[0] : 0f,
                pivots.Length > 1 ? pivots[1] : 0f,
                pivots.Length > 2 ? pivots[2] : 0f,
                pivots.Length > 3 ? pivots[3] : 0f);
        }

        private static FanlightRandomData[] BuildRandomData(FanlightRuntimeLayout layout, uint seed)
        {
            var data = new FanlightRandomData[layout.SeatCount];

            for (var i = 0; i < data.Length; i++)
            {
                var stableSeatId = layout.StableSeatIds[i];
                data[i] = new FanlightRandomData
                {
                    random0 = Random4(seed, stableSeatId, 0u),
                    random1 = Random4(seed, stableSeatId, 4u),
                    random2 = Random4(seed, stableSeatId, 8u),
                    random3 = Random4(seed, stableSeatId, 12u),
                    random4 = Random4(seed, stableSeatId, 16u),
                    random5 = Random4(seed, stableSeatId, 20u),
                    random6 = Random4(seed, stableSeatId, 24u),
                    random7 = Random4(seed, stableSeatId, 28u)
                };
            }

            return data;
        }

        private static uint[] BuildStableAssignments(FanlightRuntimeLayout layout, uint seed)
        {
            var assignments = new uint[layout.SeatCount];
            for (var i = 0; i < assignments.Length; i++)
            {
                var stableSeatId = layout.StableSeatIds[i];
                var laneBase = unchecked((uint)FanlightPenlightAssignment.PersonaAlgorithmVersion) * 64u;
                var paletteRandom = Random01(seed, stableSeatId, laneBase + 27u);
                var intensityRandom = Random01(seed, stableSeatId, laneBase + 28u);
                var paletteIndex = (uint)Mathf.Clamp(
                    Mathf.FloorToInt(paletteRandom * PaletteSlotCount),
                    0,
                    PaletteSlotCount - 1);
                var packedIntensity = (uint)Mathf.Clamp(Mathf.FloorToInt(intensityRandom * 65536.0f), 0, 65535);
                assignments[i] = paletteIndex | (packedIntensity << 8);
            }

            return assignments;
        }

        private static Vector4 Random4(uint globalSeed, ulong stableSeatId, uint offset)
        {
            return new Vector4(
                Random01(globalSeed, stableSeatId, offset + 0u),
                Random01(globalSeed, stableSeatId, offset + 1u),
                Random01(globalSeed, stableSeatId, offset + 2u),
                Random01(globalSeed, stableSeatId, offset + 3u));
        }

        private static float Random01(uint globalSeed, ulong stableSeatId, uint lane)
        {
            var x = globalSeed ^ 0x9E3779B9u;
            x ^= (uint)stableSeatId + 0x85EBCA6Bu + (x << 6) + (x >> 2);
            x ^= (uint)(stableSeatId >> 32) + 0x27D4EB2Fu + (x << 6) + (x >> 2);
            x ^= lane + 0xC2B2AE35u + (x << 6) + (x >> 2);
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;
            return (x & 0x00FFFFFFu) / 16777215.0f;
        }

        internal void Release()
        {
            SeatBuffer?.Release();
            BlockBuffer?.Release();
            BlockVisibilityBuffer?.Release();
            PenlightVisibleIndexBuffer?.Release();
            PenlightVariantAssignmentBuffer?.Release();
            PenlightVariantOffsetBuffer?.Release();
            AudienceVisibleIndexBuffer?.Release();
            MatrixBuffer?.Release();
            StableAssignmentBuffer?.Release();
            ResolvedChromaBuffer?.Release();
            ResolvedMaskBuffer?.Release();
            RuntimeBlockPaletteBuffer?.Release();
            RuntimeBlockPulseGroupBuffer?.Release();
            RandomBuffer?.Release();
            MotionSampleBuffer?.Release();
            PenlightArgsBuffer?.Release();
            AudiencePartBuffer?.Release();
            AudienceArgsBuffer?.Release();

            SeatBuffer = null;
            BlockBuffer = null;
            BlockVisibilityBuffer = null;
            PenlightVisibleIndexBuffer = null;
            PenlightVariantAssignmentBuffer = null;
            PenlightVariantOffsetBuffer = null;
            AudienceVisibleIndexBuffer = null;
            MatrixBuffer = null;
            StableAssignmentBuffer = null;
            ResolvedChromaBuffer = null;
            ResolvedMaskBuffer = null;
            RuntimeBlockPaletteBuffer = null;
            RuntimeBlockPulseGroupBuffer = null;
            RandomBuffer = null;
            MotionSampleBuffer = null;
            PenlightArgsBuffer = null;
            AudiencePartBuffer = null;
            AudienceArgsBuffer = null;
            SeatCount = 0;
            BlockCount = 0;
            LocalBounds = default;
            PenlightVariantCount = 0;
            PenlightVariantOffsets = Array.Empty<uint>();
            PenlightVariantGripPivotYs = default;
            _runtimeBlockPaletteSlots = Array.Empty<uint>();
            _runtimeBlockPaletteCandidate = Array.Empty<uint>();
            _runtimeBlockPaletteAssigned = Array.Empty<bool>();
            Array.Clear(_runtimeBlockPaletteUploaded, 0, _runtimeBlockPaletteUploaded.Length);
            _runtimeBlockPulseGroups = Array.Empty<uint>();
            _runtimeBlockPulseGroupCandidate = Array.Empty<uint>();
            _runtimeBlockPulseGroupAssigned = Array.Empty<bool>();
            Array.Clear(_runtimeBlockPulseGroupsUploaded, 0, _runtimeBlockPulseGroupsUploaded.Length);
            Array.Clear(_motionSamples, 0, _motionSamples.Length);
            Array.Clear(_motionSourceSamples, 0, _motionSourceSamples.Length);
            Array.Clear(_motionAssets, 0, _motionAssets.Length);
            Array.Clear(_motionRevisions, 0, _motionRevisions.Length);
            _motionReferencePose = default;
            _motionWeights = default;
            _hasMotionData = false;
        }
    }
}
