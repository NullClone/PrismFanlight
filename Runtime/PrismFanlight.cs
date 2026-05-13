using PrismFanlight.Rendering;
using UnityEngine;

namespace PrismFanlight
{
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
        private bool _enableCulling = true;

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

        private readonly FanlightGpuRenderer _renderer = new();


        // Properties

        public bool IsGpuReady => _renderer.IsReady;

        public int GpuSeatCount => _renderer.SeatCount;

        public int GpuVisibleSeatCount => _renderer.LastVisibleSeatCount;

        public int GpuCulledSeatCount => _renderer.LastCulledSeatCount;

        public int GpuBlockCount => _renderer.BlockCount;

        public int GpuInstanceThreadGroups => _renderer.InstanceThreadGroups;

        public int GpuBlockThreadGroups => _renderer.BlockThreadGroups;

        public long GpuBufferMemoryBytes => _renderer.BufferMemoryBytes;

        public bool IsCullingEnabled => _enableCulling;


        // Methods

        private void OnEnable()
        {
            _cullingCamera ??= Camera.main;
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
        }

        private void OnDisable()
        {
            _renderer.Dispose();
        }

        private void Update()
        {
            _renderer.Render(
                _mesh,
                _material,
                _computeShader,
                _cullingCamera,
                _enableCulling,
                _audience,
                GetMotion(),
                GetColorSettings(),
                transform.localToWorldMatrix,
                Time.time);
        }


        public Audience GetAudience() => _audience.Validated();

        public FanlightMotionSettings GetMotion() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();
    }
}
