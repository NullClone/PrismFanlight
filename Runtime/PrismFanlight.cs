using System;
using PrismFanlight.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PrismFanlight
{
    [AddComponentMenu("Prism Fanlight/Prism Fanlight")]
    [ExecuteAlways]
    public sealed class PrismFanlight : MonoBehaviour
    {
        // Fields

        [SerializeField]
        private Mesh _mesh = null;

        [SerializeField]
        private Material _material = null;

        [SerializeField]
        private Material _audienceMaterial = null;

        [SerializeField]
        private ComputeShader _computeShader = null;

        [SerializeField]
        private uint _renderingLayerMask = 1u;

        [SerializeField]
        private bool _enableCulling = true;

        [SerializeField]
        private Camera _cullingCamera = null;

        [SerializeField]
        private FanlightGpuUpdateTiming _visibilityUpdate = FanlightGpuUpdateTiming.EveryFrame();

        [SerializeField]
        private FanlightGpuUpdateTiming _animationUpdate = FanlightGpuUpdateTiming.EveryFrame();

        [SerializeField]
        private SeatLayout _seatLayout = SeatLayout.Default();

        [SerializeField]
        private FanlightMotionPreset _motionPreset = null;

        [SerializeField]
        private FanlightMotionSettings _motion = FanlightMotionSettings.Default();

        [SerializeField]
        private FanlightColorPreset _colorPreset = null;

        [SerializeField]
        private FanlightColorSettings _color = FanlightColorSettings.Default();

        [SerializeField]
        private FanlightAudienceSettings _audienceSettings = FanlightAudienceSettings.Default();

        [SerializeField]
        private FanlightLodSettings _lod = FanlightLodSettings.Default();

        [SerializeField]
        private FanlightRandomSettings _random = FanlightRandomSettings.Default();

        [SerializeField]
        private FanlightTempoSettings _tempo = FanlightTempoSettings.Default();

        [SerializeField]
        private Transform _swingTarget = null;

        private readonly FanlightGpuRenderer _renderer = new();
        private bool _hasResolvedStateOverride;
        private bool _overrideTimeJumpPending;
        private FanlightResolvedState _resolvedStateOverride;


        // Properties

        public Mesh Mesh
        {
            get => _mesh;
            set => _mesh = value;
        }

        public Camera CullingCamera
        {
            get => _cullingCamera;
            set => _cullingCamera = value;
        }

        public Transform SwingTarget
        {
            get => _swingTarget;
            set => _swingTarget = value;
        }

        public bool Enable => enabled && SystemInfo.supportsComputeShaders;

        public bool IsCullingEnabled => _enableCulling && Application.isPlaying;

        public uint RenderingLayerMask => _renderingLayerMask;

        public FanlightGpuUpdateTiming VisibilityUpdate => _visibilityUpdate.Validated();

        public FanlightGpuUpdateTiming AnimationUpdate => _animationUpdate.Validated();

        public FanlightTempoSettings Tempo => _tempo.Validated();

        public FanlightMotionPreset MotionPreset => _motionPreset;

        public FanlightColorPreset ColorPreset => _colorPreset;

        public FanlightLodSettings Lod => _lod.Validated();

        public FanlightRandomSettings Random => _random.Validated();

        internal bool HasResolvedStateOverride => _hasResolvedStateOverride;

        internal static event Action<PrismFanlight> ResolvedStateOverrideChanged;


        // Methods

        private void Start()
        {
            if (!Application.isPlaying) return;

            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("Compute shaders are not supported on this platform.");

                return;
            }

            if (_enableCulling && _cullingCamera == null && Camera.main != null)
            {
                _cullingCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (!Enable) return;

            if (_hasResolvedStateOverride)
            {
                Render(_resolvedStateOverride, _overrideTimeJumpPending);
                _overrideTimeJumpPending = false;

                return;
            }

            if (!Application.isPlaying) return;

            var context = FanlightEvaluationContext.Runtime(GetCurrentTime(), GetCurrentUpdateClock());
            var state = ResolveState(context);

            Render(state);
        }

        private void OnDisable()
        {
            ClearResolvedStateOverride();
            Dispose();
        }

        private void OnDestroy()
        {
            ClearResolvedStateOverride();
            Dispose();
        }

        private float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return (float)EditorApplication.timeSinceStartup;
#endif
            return Time.time;
        }

        private float GetCurrentUpdateClock()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return (float)EditorApplication.timeSinceStartup;
#endif
            return Time.unscaledTime;
        }


        public void Render(FanlightResolvedState state)
        {
            Render(state, state.IsTimeJump);
        }

        private void Render(FanlightResolvedState state, bool isTimeJump)
        {
            if (!Enable) return;

            var cameraPosition = _cullingCamera != null ? _cullingCamera.transform.position : transform.position;

            _renderer.Render(
                _mesh,
                _material,
                _computeShader,
                _renderingLayerMask,
                _cullingCamera,
                IsCullingEnabled,
                VisibilityUpdate,
                AnimationUpdate,
                GetSeatLayout(),
                _audienceMaterial,
                state,
                isTimeJump,
                cameraPosition);
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }


        public SeatLayout GetSeatLayout() => (_seatLayout ?? SeatLayout.Default()).Validated();

        public FanlightMotionSettings GetMotionSettings() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();

        public FanlightAudienceSettings GetAudienceSettings() => _audienceSettings.Validated();

        public FanlightLodSettings GetLodSettings() => _lod.Validated();

        public FanlightRandomSettings GetRandomSettings() => _random.Validated();

        public FanlightTempoState GetTempoState() => Tempo.Evaluate(Time.time);

        public FanlightDiagnostics GetDiagnostics()
        {
            var layout = GetSeatLayout();
            var blockCount = layout.blockCount.x * layout.blockCount.y;

            return new FanlightDiagnostics(
                _renderer.IsReady,
                layout.TotalSeatCount,
                _renderer.VisibleSeatCount,
                blockCount);
        }


        public void SetResolvedStateOverride(FanlightResolvedState state)
        {
            _resolvedStateOverride = state;
            _hasResolvedStateOverride = true;
            _overrideTimeJumpPending = state.IsTimeJump;
            ResolvedStateOverrideChanged?.Invoke(this);
        }

        internal FanlightResolvedState ResolveState(FanlightEvaluationContext context)
        {
            var tempo = Tempo;
            if (context.Source == FanlightEvaluationSource.Timeline)
            {
                tempo.clockSource = FanlightTempoClockSource.ManualTime;
                tempo.manualTime = Mathf.Max(0.0f, context.Time);
            }

            return new FanlightResolvedState(
                tempo.Evaluate(context.Time),
                GetMotionSettings(),
                GetColorSettings(),
                GetAudienceSettings(),
                GetLodSettings(),
                GetRandomSettings(),
                _swingTarget != null ? _swingTarget.position : Vector3.zero,
                transform.localToWorldMatrix,
                context.Time,
                context.UpdateClock,
                context.IsTimeJump);
        }

        public void ClearResolvedStateOverride()
        {
            if (!_hasResolvedStateOverride) return;

            _hasResolvedStateOverride = false;
            _overrideTimeJumpPending = false;
            _resolvedStateOverride = default;
            ResolvedStateOverrideChanged?.Invoke(this);
        }


        public void SetRenderingLayerMask(uint renderingLayerMask)
        {
            _renderingLayerMask = renderingLayerMask;
        }

        public void SetVisibilityUpdate(FanlightGpuUpdateTiming timing)
        {
            _visibilityUpdate = timing.Validated();
        }

        public void SetAnimationUpdate(FanlightGpuUpdateTiming timing)
        {
            _animationUpdate = timing.Validated();
        }

        public void SetSeatLayout(SeatLayout layout)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Seat layout changes are editor-only. Bake the layout before entering Play mode.");
                return;
            }

            _seatLayout = (layout ?? SeatLayout.Default()).Validated();

            Dispose();
        }

        public void SetTempo(FanlightTempoSettings tempo)
        {
            _tempo = tempo.Validated();
        }

        public void SetBpm(float bpm)
        {
            _tempo.bpm = Mathf.Max(1.0f, bpm);
        }

        public void SetMotion(FanlightMotionSettings motion)
        {
            _motionPreset = null;
            _motion = motion.Validated();
        }

        public void SetMotionPreset(FanlightMotionPreset preset)
        {
            _motionPreset = preset;
        }

        public void SetColorSettings(FanlightColorSettings color)
        {
            _colorPreset = null;
            _color = color.Validated();
        }

        public void SetColorPreset(FanlightColorPreset preset)
        {
            _colorPreset = preset;
        }

        public void SetAudienceSettings(FanlightAudienceSettings audience)
        {
            _audienceSettings = audience.Validated();
        }

        public void SetLodSettings(FanlightLodSettings lod)
        {
            _lod = lod.Validated();
        }

        public void SetRandomSettings(FanlightRandomSettings random)
        {
            _random = random.Validated();
        }


#if UNITY_EDITOR
        public void BakeSeatLayoutForEditor()
        {
            if (Application.isPlaying) return;

            var layout = GetSeatLayout();
            layout.SetBakedGeometry(
                FanlightGeometryBuilder.BuildSeatData(layout, false),
                FanlightGeometryBuilder.BuildBakedBlockData(layout),
                FanlightGeometryBuilder.BuildAuthoringBounds(layout));

            _seatLayout = layout;

            Dispose();
        }

        public void ClearSeatLayoutBakeForEditor()
        {
            if (Application.isPlaying) return;

            _seatLayout = GetSeatLayout();
            _seatLayout.ClearBakedGeometry();

            Dispose();
        }
#endif
    }
}
