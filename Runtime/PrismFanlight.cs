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


        // Methods

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
                _audience,
                GetMotion(),
                GetColorSettings(),
                transform.localToWorldMatrix,
                Time.time);
        }


        public Audience GetAudience() => _audience;

        public FanlightMotionSettings GetMotion() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();
    }
}
