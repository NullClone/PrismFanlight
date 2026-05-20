# Prism Fanlight Project Overview

This document is the handoff map for Prism Fanlight. It summarizes what has been built, why the current architecture looks the way it does, and what should be implemented next.

## Goal

Prism Fanlight is a Unity tool for rendering a large 3D live-event audience holding penlights. The direction is a production-ready asset that supports:

- high instance counts
- GPU-driven rendering
- SceneView placement preview
- fine runtime control for motion and color
- BPM-synchronized motion for music-driven staging
- reusable presets
- future Timeline/show cue workflows

## Current Status

The project has moved from a CPU Job + `Graphics.RenderMeshInstanced` prototype to a GPU-driven renderer.

Current runtime path:

```text
PrismFanlight MonoBehaviour
  -> FanlightGpuRenderer
      -> FanlightGpuBuffers
      -> FanlightGpuDispatcher
      -> ComputeShader:
          ClearIndirectArgs
          CullBlocks
          BuildVisibleInstances
          GenerateVisibleAnimation
          GenerateAllAnimation
      -> Graphics.DrawMeshInstancedIndirect
```

Current GPU features:

- Seat data is generated on CPU only when the layout changes.
- Seat data is uploaded to GPU buffers.
- Block data is generated for GPU culling.
- Block-level frustum culling runs in ComputeShader.
- Visible seat indices are generated on GPU.
- Indirect draw args are updated on GPU.
- Matrix and color are generated on GPU with a separate update cadence from visibility.
- Optional BPM beat data is generated on CPU and passed to the GPU for motion synchronization.
- Debug readback samples visible instance count every 10 frames.
- GPU culling can be toggled on/off from the Inspector.

## Runtime Components

### `PrismFanlight`

Main MonoBehaviour.

Responsibilities:

- stores user-facing references and local settings
- owns the `FanlightGpuRenderer`
- resolves motion/color settings from local values or presets
- passes mesh/material/compute/camera/settings to the renderer
- evaluates tempo/song time and passes beat data to the renderer

Important serialized fields:

- `_mesh`
- `_material`
- `_computeShader`
- `_enableCulling`
- `_visibilityUpdate`
- `_animationUpdate`
- `_tempo`
- `_cullingCamera`
- `_audience`
- `_motionPreset`
- `_motion`
- `_colorPreset`
- `_color`

### `Audience`

Layout settings and deterministic seat position helpers.

Responsibilities:

- seat count calculation
- block/seat coordinate lookup
- local plane position generation
- validation of layout values

This is still a rectangular block layout. Future placement tools should expand beyond this without breaking the GPU renderer.

### `FanlightMotionSettings`

Motion parameters for penlight swing.

Current groups:

- Timing: frequency, random phase, phase noise, reaction delay, tempo drift
- Swing Shape: arm length, angle range, snap, hold, flick, return bias
- Direction / Axis: base axis, forward/back amount, vertical amount, axis randomness/noise
- Variation: seat, height, and arm length jitter
- Humanization: enthusiasm, rest amount/intensity, cyclic rest timing, rest fade, small-motion ratio
- BPM Sync: beat sync amount, beats per swing, beat phase offset, downbeat accent, beat reaction delay, seat jitter, block delay

The CPU method remains useful as a reference, but runtime rendering now uses GPU-side equivalents in HLSL.

Motion presets should be built by combining these parameters rather than adding fixed motion-pattern enums. This keeps the shader path flexible and avoids hard-coded pattern branches.

Rest behavior has two modes:

- With rest cycle duration or rest duration at zero, rest candidates stay at `restIntensity`.
- With both set, rest candidates periodically enter a reduced-motion state and fade back in. `restPhaseRandomness` offsets rest timing per seat so the audience does not rest in unison.

### `FanlightTempoSettings`

Tempo and song-position settings used by BPM-synchronized motion.

Current clock sources:

- Unity time
- AudioSource time, using `timeSamples` when possible
- Manual time

Responsibilities:

- calculate song time after offset and latency compensation
- calculate beat, beat phase, and bar phase from BPM
- expose clock readiness so AudioSource-based sync fails visibly instead of silently falling back
- keep BPM sync optional so legacy frequency-based motion remains unchanged

Tempo currently drives motion only. Color/effect synchronization should be added separately when color and motion update lanes are split.

BPM motion supports per-seat beat reaction delay, random beat jitter, and signed block delay. These controls keep motion musically locked while preventing the audience from looking mechanically identical.

### `FanlightColorSettings`

Color/effect parameters.

Supported modes:

- Solid
- RandomHue
- Rainbow
- Wave
- RadialWave
- BlockGradient

The CPU method remains useful as a reference, but runtime rendering now uses GPU-side equivalents in HLSL.

## Rendering Folder

### `FanlightGpuRenderer`

Thin orchestration class.

Responsibilities:

- validate render inputs
- initialize GPU resources when mesh/layout changes
- build dispatch context
- call compute dispatcher
- perform debug readback
- issue `Graphics.DrawMeshInstancedIndirect`

This file should remain small. Do not add buffer setup, geometry building, shader property ids, or dispatch details back into this class.

### `FanlightGpuBuffers`

Owns GPU buffers.

Buffers:

- seat buffer
- block buffer
- block visibility buffer
- visible index buffer
- matrix buffer
- color buffer
- indirect args buffer

Responsibilities:

- allocate/release buffers
- upload static seat/block data
- reset indirect args
- estimate GPU buffer memory

### `FanlightGeometryBuilder`

CPU-side static data generation.

Responsibilities:

- build `FanlightSeatData[]`
- build `FanlightBlockData[]`
- build local rendering bounds
- transform bounds to world space
- calculate max transform scale for culling radius

### `FanlightGpuDispatcher`

Compute dispatch and parameter binding.

Responsibilities:

- bind common params
- bind beat/tempo params
- bind culling planes
- dispatch `ClearIndirectArgs`
- dispatch `CullBlocks`
- dispatch `BuildVisibleInstances`
- dispatch `GenerateVisibleAnimation`
- dispatch `GenerateAllAnimation`

This is the right place to add future LOD dispatches or split kernels.

### `FanlightGpuUpdateScheduler`

Separates GPU update cadence per pipeline stage.

Current lanes:

- visibility: indirect args, block culling, visible index list
- animation/color: matrix and per-seat color buffers

For live cameras, visibility should usually stay on `EveryFrame`. Animation/color can be set to `FixedRate` to reduce GPU write cost while rendering still happens every frame.

### `FanlightShaderIds`

Centralized shader property ids.

Do not scatter `Shader.PropertyToID` calls throughout renderer code.

### `FanlightGpuDebugReadback`

Lightweight debug readback.

Currently reads `_DrawArgs[1]`, the visible instance count, every 10 frames using `AsyncGPUReadback`.

Avoid reading GPU data every frame unless actively debugging.

### `FanlightSeatData`

GPU data structs:

- `FanlightSeatData`
- `FanlightBlockData`

Keep C# layout and HLSL layout in sync with `PrismFanlightTypes.hlsl`.

## Shader Files

### `PrismFanlightIndirect.compute`

ComputeShader entry point.

Kernels:

- `ClearIndirectArgs`
- `CullBlocks`
- `BuildVisibleInstances`
- `GenerateVisibleAnimation`
- `GenerateAllAnimation`

This file should stay focused on kernel flow. Shared math, culling, and animation logic lives in include files.

### `PrismFanlightTypes.hlsl`

GPU struct definitions.

Must match C# structs:

- `FanlightSeatData`
- `FanlightBlockData`

### `PrismFanlightMath.hlsl`

Shared math helpers:

- hash
- noise
- HSV conversion
- transform matrix helpers

### `PrismFanlightCulling.hlsl`

Frustum culling helpers.

Current implementation:

- sphere vs frustum planes

Future candidate:

- AABB/OBB block culling

### `PrismFanlightAnimation.hlsl`

GPU motion and color generation.

Current functions:

- `PrismComputeMatrix`
- `PrismComputeColor`

`PrismComputeMatrix` can blend between legacy Hz-based phase and BPM beat phase. It also evaluates per-seat cyclic rest on the GPU. `PrismComputeColor` still uses normal time-based color animation and does not consume tempo data yet.

### `PrismFanlightIndirect.shader`

Indirect draw shader.

Current behavior:

- reads `_VisibleIndices[SV_InstanceID]`
- uses visible index to read matrix/color buffers
- renders additive unlit penlights

This shader is intentionally simple for correctness and performance. URP-specific production shader variants can be added later.

## Editor Components

### `PrismFanlightEditor`

Custom Inspector.

Current sections:

- Rendering
- Layout
- Tempo
- Motion
- Color
- Debug

Debug displays:

- total seats
- blocks
- visible seats
- BPM sync on/off
- tempo clock readiness
- song time
- beat position
- bar/beat index
- culled seats
- culling ratio
- thread groups
- buffer memory
- culling on/off

### `PrismFanlightScenePreview`

SceneView placement preview.

Responsibilities:

- draw seat dots
- draw block outlines
- cap preview count for editor responsiveness

Runtime rendering does not depend on SceneView preview.

### `PrismFanlightPresetUtility`

Creates and assigns preset assets.

Current preset types:

- motion preset
- color preset

Earlier layout preset support existed, but the current `PrismFanlight` stores layout locally. Reintroduce layout presets carefully if needed.

## Current Important Behavior

### GPU Culling On

```text
ClearIndirectArgs
CullBlocks using camera frustum
BuildVisibleInstances only for visible blocks
GenerateVisibleAnimation or GenerateAllAnimation depending on update state
Draw visible instances
```

Requires a culling camera. If the serialized camera is not set, `PrismFanlight` tries `Camera.main` on enable.

### GPU Culling Off

```text
ClearIndirectArgs
CullBlocks marks all blocks visible
BuildVisibleInstances for all seats
GenerateVisibleAnimation or GenerateAllAnimation depending on update state
Draw all instances
```

Useful for debugging culling errors and comparing visible counts.

### Update Timing

Visibility and animation/color now have independent update timing.

```text
Visibility Update:
  ClearIndirectArgs
  CullBlocks
  BuildVisibleInstances

Animation / Color Update:
  GenerateVisibleAnimation during normal scheduled updates
  GenerateAllAnimation on first initialization or transform changes
```

This keeps camera-dependent culling responsive while allowing expensive matrix/color writes to be throttled.

## Known Limitations

- Shader compilation is not verified by `dotnet build`; Unity Editor import is required.
- Current culling is block sphere culling, not AABB/OBB.
- Current renderer has one LOD only.
- Matrix buffer stores `float4x4` per seat, which is expensive at very high counts.
- Color and motion are generated together, but their dispatch timing is independent from visibility.
- Debug readback is intentionally delayed and sampled every 10 frames.
- Current shader is a simple indirect unlit shader, not a final URP/HDRP production shader.
- Placement editing is still basic; SceneView preview exists, but there is no full placement editor window yet.

## Recommended Next Work

### 1. Unity Validation Pass

Before adding major features, verify in Unity Editor:

- compute shader imports correctly
- HLSL includes resolve
- material uses `Prism Fanlight/Indirect Unlit`
- culling on/off behaves as expected
- visible count changes when camera moves
- no unexpected disappearing from bounds/culling

### 2. LOD0 + LOD2 Billboard

Highest performance return after GPU culling.

Plan:

- add LOD settings
- create billboard mesh/material
- create LOD0 visible indices and args
- create LOD2 visible indices and args
- compute distance from camera
- draw mesh LOD and billboard LOD separately

### 3. Render Settings Asset

Create `FanlightRenderSettings : ScriptableObject`.

Candidate fields:

- enable culling
- culling camera override
- debug readback interval
- bounds padding
- enable LOD
- LOD distances
- billboard size
- update throttling settings

### 4. Matrix Buffer Compression

Replace per-instance `float4x4` with compressed transform data.

Potential layout:

```text
position: float3
axis: float3 or packed
angle: float
color: half4 or float4
```

This will reduce GPU write bandwidth and memory usage.

### 5. Color/Motion Update Separation

Split color and motion into separate kernels so static color modes do not update every animation tick.

Candidates:

- Solid
- BlockGradient
- RandomHue with zero speed

### 6. Better Culling

Improve from sphere culling to AABB or OBB.

Recommended next step:

- AABB block culling

### 7. Show/Cue System

Add production live-control workflow:

- cues
- durations
- transitions
- preset blending
- Timeline integration later

## Handoff Rules

- Keep `FanlightGpuRenderer` small.
- Add new GPU buffers in `FanlightGpuBuffers`.
- Add new dispatch logic in `FanlightGpuDispatcher`.
- Add new shader ids in `FanlightShaderIds`.
- Add reusable HLSL logic to include files, not directly into `.compute`.
- Keep C# GPU structs and `PrismFanlightTypes.hlsl` synchronized.
- Avoid CPU readbacks except for sampled diagnostics.
- Prefer adding settings through a future render settings asset rather than expanding `PrismFanlight` indefinitely.
