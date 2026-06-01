using PrismFanlight.Rendering;
using UnityEngine;

namespace PrismFanlight
{
    [AddComponentMenu("Prism Fanlight/Prism Fanlight")]
    public sealed class PrismFanlight : MonoBehaviour
    {
        // Fields

        [SerializeField]
        private Mesh _mesh = null;

        [SerializeField]
        private Material _material = null;

        [SerializeField]
        private ComputeShader _computeShader = null;

        [SerializeField]
        private uint _renderingLayerMask = 1u;

        [SerializeField]
        private bool _enableCulling = true;

        [SerializeField]
        private FanlightGpuUpdateTiming _visibilityUpdate = FanlightGpuUpdateTiming.EveryFrame();

        [SerializeField]
        private FanlightGpuUpdateTiming _animationUpdate = FanlightGpuUpdateTiming.EveryFrame();

        [SerializeField]
        private FanlightTempoSettings _tempo = FanlightTempoSettings.Default();

        [SerializeField]
        private Camera _cullingCamera = null;

        [SerializeField]
        private Audience _audience = Audience.Default();

        [SerializeField]
        private FanlightMotionPreset _motionPreset = null;

        [SerializeField]
        private FanlightMotionSettings _motion = FanlightMotionSettings.Default();

        [SerializeField]
        private FanlightColorPreset _colorPreset = null;

        [SerializeField]
        private FanlightColorSettings _color = FanlightColorSettings.Default();

        [SerializeField]
        private Transform _swingTarget = null;

        private readonly FanlightGpuRenderer _renderer = new();


        // Properties

        public bool IsCullingEnabled => _enableCulling;

        public uint RenderingLayerMask => _renderingLayerMask;

        public Mesh Mesh
        {
            get => _mesh;
            set => _mesh = value;
        }

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        public Camera CullingCamera
        {
            get => _cullingCamera;
            set => _cullingCamera = value;
        }

        public ComputeShader ComputeShader => _computeShader;

        public Transform SwingTarget
        {
            get => _swingTarget;
            set => _swingTarget = value;
        }

        public FanlightGpuUpdateTiming VisibilityUpdate => _visibilityUpdate.Validated();

        public FanlightGpuUpdateTiming AnimationUpdate => _animationUpdate.Validated();

        public FanlightTempoSettings Tempo => _tempo.Validated();

        public FanlightMotionPreset MotionPreset => _motionPreset;

        public FanlightColorPreset ColorPreset => _colorPreset;


        // Methods

        private void Start()
        {
            if (_enableCulling && _cullingCamera == null && Camera.main != null)
            {
                _cullingCamera = Camera.main;
            }
        }

        private void Update()
        {
            _renderer.Render(
                _mesh,
                _material,
                _computeShader,
                _renderingLayerMask,
                _cullingCamera,
                _enableCulling,
                VisibilityUpdate,
                AnimationUpdate,
                GetTempoState(),
                GetAudience(),
                GetMotion(),
                GetColorSettings(),
                _swingTarget != null ? _swingTarget.position : Vector3.zero,
                transform.localToWorldMatrix,
                Time.time,
                Time.unscaledTime);
        }

        public Audience GetAudience() => (_audience ?? Audience.Default()).Validated();

        public FanlightMotionSettings GetMotion() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();

        public FanlightTempoState GetTempoState() => Tempo.Evaluate(Time.time);

        public FanlightDiagnostics GetDiagnostics()
        {
            var audience = GetAudience();
            var blockCount = audience.blockCount.x * audience.blockCount.y;

            return new FanlightDiagnostics(
                _renderer.IsReady,
                audience.TotalSeatCount,
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

        public void SetAudience(Audience audience)
        {
            _audience = (audience ?? Audience.Default()).Validated();
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

        public void SetRenderingLayerMask(uint renderingLayerMask)
        {
            _renderingLayerMask = renderingLayerMask;
        }

        private void OnDisable()
        {
            _renderer.Dispose();
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
        }
    }
}
