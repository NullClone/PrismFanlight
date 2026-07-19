using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Rendering;
using PrismFanlight.Time;
using UnityEngine;
using UnityEngine.Playables;

namespace PrismFanlight
{
    [HelpURL("https://github.com/NullClone/PrismFanlight")]
    [AddComponentMenu("Prism Fanlight/Prism Fanlight")]
    [ExecuteAlways]
    public sealed class PrismFanlight : MonoBehaviour
    {
        // Fields

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
        private SeatLayout _seatLayout = SeatLayout.Default();

        [SerializeField]
        private FanlightLayoutAsset _layoutAsset;

        [SerializeField]
        private Transform _swingTarget;

        [SerializeField]
        private ShowTimeCoordinatorBehaviour _timeCoordinator;

        [SerializeField]
        private PlayableDirector _timelineDirector;

        [SerializeField]
        private FanlightIntentState _intent = FanlightShowStateDefaults.Intent();

        [SerializeField]
        private FanlightGestureState _gesture = FanlightShowStateDefaults.Gesture();

        [SerializeField]
        private FanlightPoseState _pose = FanlightShowStateDefaults.Pose();

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
        private FanlightPaletteState _palette = FanlightShowStateDefaults.Palette();

        [SerializeField]
        private FanlightVisibilityState _visibility = FanlightShowStateDefaults.Visibility();

        [SerializeField]
        private uint _globalSeed = 1u;


        private readonly FanlightGpuRenderer _renderer = new();
        private readonly Dictionary<object, FanlightShowContribution> _scheduledContributions = new();
        private readonly FanlightContributionBuffer _contributionBuffer = new(16);
        private readonly FanlightShowEvaluator _showEvaluator = new();
        private long _evaluationId;
        private SeatLayout _validatedSeatLayout;
        private FanlightRuntimeLayout _legacyRuntimeLayout;
        private FanlightRuntimeLayout _assetRuntimeLayout;


#if UNITY_EDITOR
        private FanlightRuntimeLayout _editorPreviewLayout;
        private bool _editorLayoutBlocked;
#endif


        // Properties

        public Camera CullingCamera
        {
            get => _cullingCamera;
            set => _cullingCamera = value;
        }

        public bool IsCullingEnabled => _enableCulling && _cullingCamera != null;

        public FanlightLayoutAsset LayoutAsset => _layoutAsset;

        private FanlightGpuUpdateTiming VisibilityUpdate => _visibilityUpdate.Validated();

        private FanlightGpuUpdateTiming AnimationUpdate => _animationUpdate.Validated();

        internal bool IsReady => _renderer.IsReady;

        internal FanlightRendererFault RendererFault => _renderer.Fault;

        internal FanlightShowState BaseState => new(
            _intent,
            _gesture,
            _pose,
            _variation,
            _noise,
            _rest,
            _audienceBody,
            _direction,
            _palette,
            _visibility,
            _globalSeed);


        // Methods

        private void Start()
        {
            if (_enableCulling && _cullingCamera == null && Camera.main != null)
            {
                _cullingCamera = Camera.main;
            }

            ResolveTimeCoordinatorReference();
        }

        private void LateUpdate()
        {
            if (!enabled || !SystemInfo.supportsComputeShaders)
            {
                Dispose();
                return;
            }

            ResolveTimeCoordinatorReference();

            if (_timeCoordinator == null || _evaluationId == long.MaxValue)
            {
                Dispose();
                return;
            }

            _evaluationId++;

            if (!_timeCoordinator.TrySample(_evaluationId, out var time, out _))
            {
                Dispose();
                return;
            }

            EvaluateTimeline(time.Seconds);

            _contributionBuffer.Clear();
            foreach (var contribution in _scheduledContributions.Values)
            {
                _contributionBuffer.Add(contribution);
            }

            var options = new FanlightEvaluationOptions(AnimationUpdate.Mode == FanlightGpuUpdateMode.FixedRate ? AnimationUpdate.TargetFrameRate : 0d, 1e-6d);
            var request = new FanlightShowEvaluationRequest(time, BaseState, _contributionBuffer.AsMemory(), options);
            var sample = _showEvaluator.Evaluate(request);

            Render(sample);
        }

        private void OnDisable()
        {
            ClearScheduledContributions();
            Dispose();
        }

        private void OnDestroy()
        {
            ClearScheduledContributions();
            Dispose();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            _validatedSeatLayout = null;
            _legacyRuntimeLayout = null;
            _assetRuntimeLayout = null;
            _editorPreviewLayout = null;
            _editorLayoutBlocked = false;

            if (_timeCoordinator == null) _timeCoordinator = GetComponent<ShowTimeCoordinatorBehaviour>();

            _intent = FanlightShowStateAuthoringValidator.Validate(_intent);
            _gesture = FanlightShowStateAuthoringValidator.Validate(_gesture);
            _pose = FanlightShowStateAuthoringValidator.Validate(_pose);
            _variation = FanlightShowStateAuthoringValidator.Validate(_variation);
            _noise = FanlightShowStateAuthoringValidator.Validate(_noise);
            _rest = FanlightShowStateAuthoringValidator.Validate(_rest);
            _audienceBody = FanlightShowStateAuthoringValidator.Validate(_audienceBody);
            _direction = FanlightShowStateAuthoringValidator.Validate(_direction);
            _palette = FanlightShowStateAuthoringValidator.Validate(_palette);
#endif
        }

        internal void SetScheduledContribution(object sourceToken, in FanlightShowContribution contribution)
        {
            if (sourceToken == null)
            {
                throw new ArgumentNullException(nameof(sourceToken));
            }

            if (contribution.Layer != FanlightContributionLayer.Timeline)
            {
                throw new ArgumentException("Timeline sources must submit Timeline contributions.", nameof(contribution));
            }

            _scheduledContributions[sourceToken] = contribution;
        }

        internal void ClearScheduledContribution(object sourceToken)
        {
            if (sourceToken != null) _scheduledContributions.Remove(sourceToken);
        }

        internal void SetLayoutAssetForEditor(FanlightLayoutAsset layoutAsset)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Layout asset changes are authoring-only. Assign and bake the layout before entering Play mode.");
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

        public SeatLayout GetSeatLayout() => GetValidatedSeatLayout();


#if UNITY_EDITOR
        public void BakeSeatLayoutForEditor()
        {
            if (Application.isPlaying) return;

            if (_layoutAsset != null)
            {
                Debug.LogWarning("Use the Layout Asset 'Bake Dirty Blocks' command for asset-backed layouts.");
                return;
            }

            var layout = GetSeatLayout();
            layout.SetBakedGeometry(
                FanlightGeometryBuilder.BuildSeatData(layout, false),
                FanlightGeometryBuilder.BuildBakedBlockData(layout),
                FanlightGeometryBuilder.BuildAuthoringBounds(layout));

            _seatLayout = layout;
            _validatedSeatLayout = _seatLayout;

            Dispose();
        }

        internal void SetEditorLayoutPreview(FanlightRuntimeLayout preview, int changedBlockIndex)
        {
            if (Application.isPlaying) return;

            if (preview == null)
            {
                _editorPreviewLayout = null;
                Dispose();
                return;
            }

            _editorLayoutBlocked = false;
            _editorPreviewLayout = preview;
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

        public void ClearSeatLayoutBakeForEditor()
        {
            if (Application.isPlaying) return;

            if (_layoutAsset != null)
            {
                Debug.LogWarning("The legacy bake cannot be cleared while a Layout Asset is assigned.");
                return;
            }

            _seatLayout = GetSeatLayout();
            _seatLayout.ClearBakedGeometry();
            _validatedSeatLayout = _seatLayout;

            Dispose();
        }
#endif

        private void EvaluateTimeline(double showSeconds)
        {
            if (_timelineDirector == null || _timelineDirector.playableAsset == null) return;

            _timelineDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            _timelineDirector.time = showSeconds;
            _timelineDirector.Evaluate();
        }

        private void Render(in FanlightShowSample sample)
        {
            var runtimeLayout = GetRuntimeLayout();

            if (runtimeLayout == null)
            {
                Dispose();
                return;
            }

            if (_computeShader == null)
            {
                throw new InvalidOperationException("A Compute Shader is required to render.");
            }

            _renderer.Load(
                runtimeLayout,
                _penlightAppearanceProfile,
                _material,
                _audienceMaterial,
                _computeShader);

            if (!_renderer.IsReady) return;

            var camera = _cullingCamera;
            var cameraPosition = camera != null ? camera.transform.position : transform.position;
            var frame = new FanlightFrameContext(
                _evaluationId,
                transform.localToWorldMatrix,
                _swingTarget != null ? _swingTarget.position : Vector3.zero);
            var cameraContext = new FanlightCameraContext(
                "camera.primary",
                camera,
                camera != null ? camera.worldToCameraMatrix : Matrix4x4.identity,
                camera != null ? camera.projectionMatrix : Matrix4x4.identity,
                cameraPosition,
                _renderingLayerMask,
                _enableCulling && camera != null);

            _renderer.Render(sample, frame, cameraContext, VisibilityUpdate, AnimationUpdate);
        }

        private void ResolveTimeCoordinatorReference()
        {
            if (_timeCoordinator == null)
            {
                _timeCoordinator = gameObject.GetComponent<ShowTimeCoordinatorBehaviour>();
            }
        }

        private void ClearScheduledContributions()
        {
            _scheduledContributions.Clear();
            _contributionBuffer.Clear();
        }

        private void Dispose()
        {
            _renderer.Dispose();
        }

        private SeatLayout GetValidatedSeatLayout()
        {
            if (_validatedSeatLayout == null)
            {
                _validatedSeatLayout = (_seatLayout ?? SeatLayout.Default()).Validated();
            }

            return _validatedSeatLayout;
        }

        private FanlightRuntimeLayout GetRuntimeLayout()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _editorLayoutBlocked) return null;
            if (!Application.isPlaying && _editorPreviewLayout != null) return _editorPreviewLayout;
#endif
            if (_layoutAsset != null)
            {
                if (_assetRuntimeLayout == null
                    || _assetRuntimeLayout.LayoutVersion != _layoutAsset.LayoutVersion
                    || _layoutAsset.ActiveBake != null && _assetRuntimeLayout.ContentHash != _layoutAsset.ActiveBake.ContentHash)
                {
                    _assetRuntimeLayout = FanlightRuntimeLayout.FromArtifact(_layoutAsset);
                }

                return _assetRuntimeLayout;
            }

            return _legacyRuntimeLayout ??= FanlightRuntimeLayout.FromLegacy(GetValidatedSeatLayout());
        }
    }
}
