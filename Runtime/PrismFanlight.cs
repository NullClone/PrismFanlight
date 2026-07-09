using PrismFanlight.Rendering;
using UnityEditor;
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

        [SerializeField]
        private bool _enablePreview = false;

        private readonly FanlightGpuRenderer _renderer = new();
        private bool _hasResolvedStateOverride;
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

        public bool EnablePreview
        {
            get => _enablePreview;
            set => _enablePreview = value;
        }

        public bool Enable => enabled && SystemInfo.supportsComputeShaders && (Application.isPlaying || (_enablePreview && IsSelectedInEditor()));

        public bool IsCullingEnabled => _enableCulling && Application.isPlaying;

        public uint RenderingLayerMask => _renderingLayerMask;

        public FanlightGpuUpdateTiming VisibilityUpdate => _visibilityUpdate.Validated();

        public FanlightGpuUpdateTiming AnimationUpdate => _animationUpdate.Validated();

        public FanlightTempoSettings Tempo => _tempo.Validated();

        public FanlightMotionPreset MotionPreset => _motionPreset;

        public FanlightColorPreset ColorPreset => _colorPreset;

        public FanlightLodSettings Lod => _lod.Validated();

        public FanlightRandomSettings Random => _random.Validated();


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

        private void Update()
        {
            if (!Enable) return;

            var state = _hasResolvedStateOverride
                ? _resolvedStateOverride
                : ResolveState(Time.time, Time.unscaledTime);

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
                ResolveLodCameraWorldPosition());
        }

        private void OnDisable()
        {
            ReleaseGpuResources();
        }

        private void OnDestroy()
        {
            ReleaseGpuResources();
        }

        private bool IsSelectedInEditor()
        {
#if UNITY_EDITOR
            return Selection.Contains(gameObject);
#else
            return false;
#endif
        }


        public SeatLayout GetSeatLayout() => (_seatLayout ?? SeatLayout.Default()).Validated();

        public bool IsSeatLayoutBakeCurrent => GetSeatLayout().HasValidBake;

        public FanlightMotionSettings GetMotion() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();

        public FanlightAudienceSettings GetAudienceSettings() => _audienceSettings.Validated();

        public FanlightLodSettings GetLodSettings() => _lod.Validated();

        public FanlightRandomSettings GetRandomSettings() => _random.Validated();

        public FanlightTempoState GetTempoState() => Tempo.Evaluate(Time.time);

        public FanlightResolvedState ResolveState(float time, float updateClock)
        {
            return new FanlightResolvedState(
                Tempo.Evaluate(time),
                GetMotion(),
                GetColorSettings(),
                GetAudienceSettings(),
                GetLodSettings(),
                GetRandomSettings(),
                _swingTarget != null ? _swingTarget.position : Vector3.zero,
                transform.localToWorldMatrix,
                time,
                updateClock);
        }

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
            ReleaseGpuResources();
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
            ReleaseGpuResources();
        }

        public void ClearSeatLayoutBakeForEditor()
        {
            if (Application.isPlaying) return;

            _seatLayout = GetSeatLayout();
            _seatLayout.ClearBakedGeometry();
            ReleaseGpuResources();
        }
#endif

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

        public void SetResolvedStateOverride(FanlightResolvedState state)
        {
            _resolvedStateOverride = state;
            _hasResolvedStateOverride = true;
        }

        public void ClearResolvedStateOverride()
        {
            _hasResolvedStateOverride = false;
        }

        public void SetRenderingLayerMask(uint renderingLayerMask)
        {
            _renderingLayerMask = renderingLayerMask;
        }

        public void ReleaseGpuResources()
        {
            _renderer.Dispose();
        }

        private Vector3 ResolveLodCameraWorldPosition()
        {
            if (_cullingCamera != null)
            {
                return _cullingCamera.transform.position;
            }

            return Camera.main != null
                ? Camera.main.transform.position
                : transform.position;
        }
    }
}
