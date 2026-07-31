using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Rendering;
using PrismFanlight.Time;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrismFanlight
{
    [ExecuteAlways]
    [HelpURL(HelpUrl)]
    [AddComponentMenu("Prism Fanlight/Prism Fanlight")]
    public sealed class PrismFanlight : MonoBehaviour
    {
        // Fields

        public const string HelpUrl = "https://github.com/NullClone/PrismFanlight";


        [SerializeField]
        private Material _material;

        [SerializeField]
        private Material _audienceMaterial;

        [SerializeField]
        private ComputeShader _computeShader;

        [SerializeField]
        private uint _renderingLayerMask = 1u;

        [SerializeField]
        private bool _enableCulling = true;

        [SerializeField]
        private Camera _cullingCamera;

        [SerializeField]
        private FanlightGpuUpdateTiming _visibilityUpdate = FanlightGpuUpdateTiming.EveryFrame();

        [SerializeField]
        private FanlightGpuUpdateTiming _animationUpdate = FanlightGpuUpdateTiming.EveryFrame();

        [SerializeField]
        private FanlightPenlightAppearanceProfile _penlightAppearanceProfile;

        [SerializeField]
        private FanlightLayoutAsset _layoutAsset;

        [SerializeField]
        private Transform _swingTarget;

        [SerializeField]
        private FanlightTimeManager _timeManager;

        [SerializeField]
        private FanlightIntentState _intent = FanlightShowStateDefaults.Intent();

        [SerializeField]
        private FanlightMotionState _motion = FanlightShowStateDefaults.Motion();

        [SerializeField]
        private FanlightVariationState _variation = FanlightShowStateDefaults.Variation();

        [SerializeField]
        private FanlightNoiseState _noise = FanlightShowStateDefaults.Noise();

        [SerializeField]
        private FanlightRestState _rest = FanlightShowStateDefaults.Rest();

        [SerializeField]
        private FanlightAudienceBodyState _audienceBody = FanlightShowStateDefaults.AudienceBody();

        [SerializeField]
        private FanlightDirectionState _direction = FanlightShowStateDefaults.Direction();

        [SerializeField]
        private FanlightColorState _color = FanlightShowStateDefaults.Color();

        [SerializeField]
        private FanlightIntensityState _intensity = FanlightShowStateDefaults.Intensity();

        [SerializeField]
        private FanlightVisibilityState _visibility = FanlightShowStateDefaults.Visibility();

        [SerializeField]
        private uint _globalSeed = 1u;


        private FanlightGpuRenderer _renderer;
        private Dictionary<object, FanlightShowContribution> _scheduledContributions;
        private FanlightContributionBuffer _contributionBuffer;
        private FanlightShowEvaluator _showEvaluator;
        private long _evaluationId;
        private long _renderFrameId;
        private FanlightShowSample _renderSample;
        private FanlightFrameContext _renderFrame;
        private bool _hasRenderFrame;
        private FanlightRuntimeLayout _assetRuntimeLayout;


#if UNITY_EDITOR
        private FanlightRuntimeLayout _editorPreviewLayout;
        private bool _editorLayoutBlocked;
#endif


        // Properties

        internal FanlightLayoutAsset LayoutAsset => _layoutAsset;

        private FanlightGpuUpdateTiming VisibilityUpdate => _visibilityUpdate.Validated();

        private FanlightGpuUpdateTiming AnimationUpdate => _animationUpdate.Validated();

        internal bool IsReady => _renderer is { IsReady: true };

        internal FanlightRendererFault RendererFault => _renderer?.Fault ?? FanlightRendererFault.MissingResource;

        internal FanlightShowState BaseState => new(
            _intent,
            _motion,
            _variation,
            _noise,
            _rest,
            _audienceBody,
            _direction,
            _color,
            _intensity,
            _visibility,
            _globalSeed);


        // Methods

#if UNITY_EDITOR
        private void Reset()
        {
            var defaultMotionAsset = Resources.Load<FanlightMotionAsset>("Default Motion Drum");
            if (defaultMotionAsset != null)
            {
                _motion = FanlightShowStateDefaults.Motion(defaultMotionAsset);
            }
        }
#endif

        private void OnEnable()
        {
            EnsureRuntimeState();

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPreCull += OnCameraPreCull;
        }

        private void Start()
        {
            if (_enableCulling && _cullingCamera == null && Camera.main != null)
            {
                _cullingCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (!enabled || !SystemInfo.supportsComputeShaders)
            {
                ClearScheduledContributions();
                Dispose();
                return;
            }

            if (_timeManager == null || _evaluationId == long.MaxValue)
            {
                ClearScheduledContributions();
                Dispose();
                return;
            }

            EnsureRuntimeState();
            _evaluationId++;

            if (!_timeManager.TrySample(_evaluationId, out var time, out _))
            {
                ClearScheduledContributions();
                Dispose();
                return;
            }

            _contributionBuffer.Clear();

            foreach (var contribution in _scheduledContributions.Values)
            {
                _contributionBuffer.Add(contribution);
            }

            var options = new FanlightEvaluationOptions(AnimationUpdate.Mode == FanlightGpuUpdateMode.FixedRate ? AnimationUpdate.TargetFrameRate : 0d, 1e-6d);
            var request = new FanlightShowEvaluationRequest(time, BaseState, _contributionBuffer.AsMemory(), options);
            var sample = _showEvaluator.Evaluate(request);

            PrepareRenderFrame(sample);
        }

        private void OnDisable()
        {
            UnregisterRenderCallbacks();
            ClearScheduledContributions();
            Dispose();
        }

        private void OnDestroy()
        {
            UnregisterRenderCallbacks();
            ClearScheduledContributions();
            Dispose();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            _assetRuntimeLayout = null;
            _editorPreviewLayout = null;
            _editorLayoutBlocked = false;

            _intent = FanlightShowStateAuthoringValidator.Validate(_intent);
            _motion = FanlightShowStateAuthoringValidator.Validate(_motion);
            _variation = FanlightShowStateAuthoringValidator.Validate(_variation);
            _noise = FanlightShowStateAuthoringValidator.Validate(_noise);
            _rest = FanlightShowStateAuthoringValidator.Validate(_rest);
            _audienceBody = FanlightShowStateAuthoringValidator.Validate(_audienceBody);
            _direction = FanlightShowStateAuthoringValidator.Validate(_direction);
#endif
        }

        internal void SetScheduledContribution(object sourceToken, in FanlightShowContribution contribution)
        {
            if (sourceToken == null)
            {
                throw new ArgumentNullException(nameof(sourceToken));
            }

            EnsureRuntimeState();
            _scheduledContributions[sourceToken] = contribution;
        }

        internal void ClearScheduledContribution(object sourceToken)
        {
            if (sourceToken != null) _scheduledContributions?.Remove(sourceToken);
        }

        internal void SetLayoutAssetForEditor(FanlightLayoutAsset layoutAsset)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Layout asset changes are authoring-only. Assign the layout before entering Play mode.");
                return;
            }

            _layoutAsset = layoutAsset;
            _assetRuntimeLayout = null;
#if UNITY_EDITOR
            _editorPreviewLayout = null;
            _editorLayoutBlocked = false;
#endif
            Dispose();
        }

        internal void SetTimeManager(FanlightTimeManager timeManager)
        {
            _timeManager = timeManager;
        }

#if UNITY_EDITOR
        internal void SetEditorLayoutPreview(FanlightRuntimeLayout preview, int changedBlockIndex)
        {
            if (preview == null)
            {
                _editorPreviewLayout = null;
                Dispose();
                return;
            }

            _editorLayoutBlocked = false;
            _editorPreviewLayout = preview;
            EnsureRuntimeState();
            _renderer.ApplyEditorLayoutPreview(preview, changedBlockIndex);
        }

        internal ulong EditorPreviewContentHash => _editorPreviewLayout?.ContentHash ?? 0UL;

        internal void SetEditorLayoutBlocked(bool blocked)
        {
            if (_editorLayoutBlocked == blocked) return;

            _editorLayoutBlocked = blocked;

            if (blocked)
            {
                _editorPreviewLayout = null;
                Dispose();
            }
        }

        internal void ClearEditorLayoutPreview()
        {
            _editorPreviewLayout = null;
            _editorLayoutBlocked = false;
            _assetRuntimeLayout = null;

            Dispose();
        }

#endif

        private void PrepareRenderFrame(in FanlightShowSample sample)
        {
            EnsureRuntimeState();
            _hasRenderFrame = false;

            var runtimeLayout = GetRuntimeLayout();

            if (runtimeLayout == null)
            {
                Dispose();
                return;
            }

            _renderer.Load(
                runtimeLayout,
                _penlightAppearanceProfile,
                _material,
                _audienceMaterial,
                _computeShader);

            if (!_renderer.IsReady) return;

            _renderSample = sample;
            _renderFrameId = _renderFrameId == long.MaxValue ? 1L : _renderFrameId + 1L;
            _renderFrame = new FanlightFrameContext(
                _renderFrameId,
                transform.localToWorldMatrix,
                _swingTarget != null ? _swingTarget.position : Vector3.zero);
            _hasRenderFrame = true;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            RenderCamera(camera);
        }

        private void OnCameraPreCull(Camera camera)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                RenderCamera(camera);
            }
        }

        private void RenderCamera(Camera camera)
        {
            if (!_hasRenderFrame || _renderer == null || !_renderer.IsReady || !ShouldRenderCamera(camera)) return;

            var cameraContext = new FanlightCameraContext(
                camera.cameraType == CameraType.SceneView ? "camera.scene-view" : "camera.primary",
                camera,
                camera.worldToCameraMatrix,
                camera.projectionMatrix,
                camera.transform.position,
                _renderingLayerMask,
                _enableCulling);

            _renderer.Render(_renderSample, _renderFrame, cameraContext, VisibilityUpdate, AnimationUpdate);
        }

        private bool ShouldRenderCamera(Camera camera)
        {
            if (camera == null) return false;
            if (camera.cameraType == CameraType.SceneView) return true;
            if (!camera.isActiveAndEnabled) return false;
            if (camera.cameraType != CameraType.Game) return false;
            return _cullingCamera == null || camera == _cullingCamera;
        }

        private void UnregisterRenderCallbacks()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Camera.onPreCull -= OnCameraPreCull;
        }

        private void ClearScheduledContributions()
        {
            _scheduledContributions?.Clear();
            _contributionBuffer?.Clear();
        }

        private void Dispose()
        {
            _hasRenderFrame = false;
            _renderSample = default;
            _renderFrame = default;
            _renderer?.Dispose();
        }

        private void EnsureRuntimeState()
        {
            _renderer ??= new FanlightGpuRenderer();
            _scheduledContributions ??= new Dictionary<object, FanlightShowContribution>();
            _contributionBuffer ??= new FanlightContributionBuffer(16);
            _showEvaluator ??= new FanlightShowEvaluator();
        }

        private FanlightRuntimeLayout GetRuntimeLayout()
        {
#if UNITY_EDITOR
            if (_editorLayoutBlocked) return null;
            if (_editorPreviewLayout != null) return _editorPreviewLayout;
#endif
            if (_layoutAsset == null || !_layoutAsset.HasValidBake)
            {
                _assetRuntimeLayout = null;
                return null;
            }

            if (_assetRuntimeLayout == null
                || _assetRuntimeLayout.ContentHash != _layoutAsset.ContentHash)
            {
                _assetRuntimeLayout = FanlightRuntimeLayout.FromArtifact(_layoutAsset);
            }

            return _assetRuntimeLayout;
        }
    }
}
