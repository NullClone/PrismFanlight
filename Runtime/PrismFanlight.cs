using System;
using System.Collections.Generic;
using PrismFanlight.Rendering;
using PrismFanlight.Timeline;
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
        private readonly Dictionary<FanlightTimelineMixerBehaviour, FanlightTimelineTrackContribution> _timelineContributions = new();
        private readonly List<FanlightTimelineTrackContribution> _sortedTimelineContributions = new();
        private SeatLayout _validatedSeatLayout;
        private bool _hasExternalResolvedStateOverride;
        private bool _externalOverrideTimeJumpPending;
        private FanlightResolvedState _externalResolvedStateOverride;


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

        internal bool HasResolvedStateOverride => _hasExternalResolvedStateOverride || _timelineContributions.Count > 0;

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

            if (_timelineContributions.Count > 0 && TryResolveTimelineState(out var timelineState))
            {
                Render(timelineState);

                return;
            }

            if (_hasExternalResolvedStateOverride)
            {
                Render(_externalResolvedStateOverride, _externalOverrideTimeJumpPending);
                _externalOverrideTimeJumpPending = false;

                return;
            }

            var context = FanlightEvaluationContext.Runtime(GetCurrentTime(), GetCurrentUpdateClock());
            var state = ResolveState(context);

            Render(state);
        }

        private void OnDisable()
        {
            ClearTimelineContributions();
            ClearResolvedStateOverride();
            Dispose();
        }

        private void OnDestroy()
        {
            ClearTimelineContributions();
            ClearResolvedStateOverride();
            Dispose();
        }

        private void OnValidate()
        {
            _validatedSeatLayout = null;
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
                GetValidatedSeatLayout(),
                _audienceMaterial,
                state,
                isTimeJump,
                cameraPosition);
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }


        public SeatLayout GetSeatLayout() => GetValidatedSeatLayout();

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
            _externalResolvedStateOverride = state;
            _hasExternalResolvedStateOverride = true;
            _externalOverrideTimeJumpPending = state.IsTimeJump;
            ResolvedStateOverrideChanged?.Invoke(this);
        }

        internal void SetTimelineContribution(FanlightTimelineMixerBehaviour source, FanlightTimelineTrackContribution contribution)
        {
            _timelineContributions[source] = contribution;
            ResolvedStateOverrideChanged?.Invoke(this);
        }

        internal void ClearTimelineContribution(FanlightTimelineMixerBehaviour source)
        {
            if (!_timelineContributions.Remove(source)) return;

            ResolvedStateOverrideChanged?.Invoke(this);
        }

        internal void ClearTimelineContributions()
        {
            if (_timelineContributions.Count == 0) return;

            _timelineContributions.Clear();
            _sortedTimelineContributions.Clear();
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
                _motionPreset != null ? _motionPreset.Settings : _motion,
                _colorPreset != null ? _colorPreset.Settings : _color,
                _audienceSettings,
                _lod,
                _random,
                _swingTarget != null ? _swingTarget.position : Vector3.zero,
                transform.localToWorldMatrix,
                context.Time,
                context.UpdateClock,
                context.IsTimeJump);
        }

        public void ClearResolvedStateOverride()
        {
            if (!_hasExternalResolvedStateOverride) return;

            _hasExternalResolvedStateOverride = false;
            _externalOverrideTimeJumpPending = false;
            _externalResolvedStateOverride = default;
            ResolvedStateOverrideChanged?.Invoke(this);
        }

        private bool TryResolveTimelineState(out FanlightResolvedState state)
        {
            _sortedTimelineContributions.Clear();
            foreach (var contribution in _timelineContributions.Values)
            {
                _sortedTimelineContributions.Add(contribution);
            }

            if (_sortedTimelineContributions.Count == 0)
            {
                state = default;
                return false;
            }

            _sortedTimelineContributions.Sort(FanlightTimelineContributionComparer.Instance);

            var time = _sortedTimelineContributions[_sortedTimelineContributions.Count - 1].Time;
            var isTimeJump = false;
            for (var i = 0; i < _sortedTimelineContributions.Count; i++)
            {
                isTimeJump |= _sortedTimelineContributions[i].IsTimeJump;
            }

            var context = FanlightEvaluationContext.Timeline(time, isTimeJump);
            var baseState = ResolveState(context);
            state = FanlightTimelineStateComposer.Compose(baseState, Tempo, _sortedTimelineContributions, time, isTimeJump);
            return true;
        }

        private sealed class FanlightTimelineContributionComparer : IComparer<FanlightTimelineTrackContribution>
        {
            public static readonly FanlightTimelineContributionComparer Instance = new();

            public int Compare(FanlightTimelineTrackContribution x, FanlightTimelineTrackContribution y)
            {
                // Timeline returns output tracks from top to bottom. Applying the
                // later entry last gives the visually lower track precedence.
                return x.SortOrder.CompareTo(y.SortOrder);
            }
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
            _validatedSeatLayout = _seatLayout;

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
            _validatedSeatLayout = _seatLayout;

            Dispose();
        }

        public void ClearSeatLayoutBakeForEditor()
        {
            if (Application.isPlaying) return;

            _seatLayout = GetSeatLayout();
            _seatLayout.ClearBakedGeometry();
            _validatedSeatLayout = _seatLayout;

            Dispose();
        }
#endif

        private SeatLayout GetValidatedSeatLayout()
        {
            if (_validatedSeatLayout == null)
            {
                _validatedSeatLayout = (_seatLayout ?? SeatLayout.Default()).Validated();
            }

            return _validatedSeatLayout;
        }
    }
}
