using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace PrismFanlight
{
    public sealed class StickShow : MonoBehaviour
    {
        private const int RenderBatchSize = 64;

        // Fields

        [SerializeField]
        private Mesh _mesh = null;

        [SerializeField]
        private Material _material = null;

        [SerializeField]
        private AudienceLayoutPreset _layoutPreset = null;

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

        private NativeArray<Matrix4x4> _matrices;
        private NativeArray<Color> _colors;
        private GraphicsBuffer _colorBuffer;
        private MaterialPropertyBlock _matProps;


        // Methods

        private void Start()
        {
            AllocateBuffers(GetAudience());
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        private void Update()
        {
            var audience = GetAudience();

            if (!CanRender(audience)) return;

            EnsureBufferCapacity(audience);
            UpdateAnimation(audience);
            Render(audience);
        }

        public Audience GetAudience() => (_layoutPreset != null ? _layoutPreset.Audience : _audience).Validated();

        public FanlightMotionSettings GetMotion() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();


        private void AllocateBuffers(Audience audience)
        {
            var seatCount = Mathf.Max(1, audience.TotalSeatCount);
            _matrices = new NativeArray<Matrix4x4>(seatCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _colors = new NativeArray<Color>(seatCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, seatCount, sizeof(float) * 4);
            _matProps = new MaterialPropertyBlock();
        }

        private void ReleaseBuffers()
        {
            if (_matrices.IsCreated) _matrices.Dispose();
            if (_colors.IsCreated) _colors.Dispose();
            _colorBuffer?.Dispose();
            _colorBuffer = null;
        }

        private void EnsureBufferCapacity(Audience audience)
        {
            if (_matrices.IsCreated && _matrices.Length == audience.TotalSeatCount) return;

            ReleaseBuffers();
            AllocateBuffers(audience);
        }

        private bool CanRender(Audience audience)
        {
            return _mesh != null
                   && _material != null
                   && audience.TotalSeatCount > 0
                   && audience.BlockSeatCount > 0;
        }

        private void UpdateAnimation(Audience audience)
        {
            Profiler.BeginSample("Prism Fanlight Animation");

            var job = new AudienceAnimationJob()
            {
                config = audience,
                motion = GetMotion(),
                color = GetColorSettings(),
                xform = transform.localToWorldMatrix,
                time = Time.time, matrices = _matrices, colors = _colors
            };

            job.Schedule(audience.TotalSeatCount, RenderBatchSize).Complete();

            Profiler.EndSample();
        }

        private void Render(Audience audience)
        {
            _colorBuffer.SetData(_colors);
            _matProps.SetBuffer("_InstanceColorBuffer", _colorBuffer);

            var rparams = new RenderParams(_material) { matProps = _matProps };

            var (i, step) = (0, audience.BlockSeatCount);

            for (var sx = 0; sx < audience.blockCount.x; sx++)
            {
                for (var sy = 0; sy < audience.blockCount.y; sy++, i += step)
                {
                    _matProps.SetInteger("_InstanceIDOffset", i);

                    Graphics.RenderMeshInstanced(rparams, _mesh, 0, _matrices, step, i);
                }
            }
        }
    }
}
