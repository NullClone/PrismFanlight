using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Live;
using PrismFanlight.Rendering;
using PrismFanlight.Time;
using UnityEngine;

namespace PrismFanlight
{
    [HelpURL("https://github.com/NullClone/PrismFanlight")]
    [AddComponentMenu("Prism Fanlight/Prism Fanlight")]
    [ExecuteAlways]
    [RequireComponent(typeof(ShowTimeCoordinatorBehaviour))]
    public sealed class PrismFanlight : MonoBehaviour
    {
        // Fields

        [SerializeField]
        private Mesh _mesh = null;

        [SerializeField]
        private FanlightPenlightAppearanceProfile _penlightAppearanceProfile = null;

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
        private FanlightLayoutAsset _layoutAsset = null;

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
        private ShowTimeCoordinatorBehaviour _timeCoordinator = null;

        [SerializeField]
        private string _showId = "show.compatibility";

        [SerializeField, Min(1)]
        private int _showVersion = 1;

        [SerializeField, HideInInspector]
        private string _sessionId = string.Empty;

        private readonly FanlightGpuRenderer _renderer = new();
        private readonly Dictionary<object, FanlightSingleContributionSource> _scheduledContributions = new();
        private readonly FanlightShowEvaluator _showEvaluator = new();
        private FanlightShowSession _showSession;
        private FanlightSingleContributionSource _externalContribution;
        private SeatLayout _validatedSeatLayout;
        private FanlightRuntimeLayout _legacyRuntimeLayout;
        private FanlightRuntimeLayout _assetRuntimeLayout;
        private bool _hasExternalResolvedStateOverride;
        private bool _externalOverrideTimeJumpPending;

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

        private bool Enable => enabled && SystemInfo.supportsComputeShaders;

        public bool IsCullingEnabled => _enableCulling && Application.isPlaying;

        private FanlightGpuUpdateTiming VisibilityUpdate => _visibilityUpdate.Validated();

        private FanlightGpuUpdateTiming AnimationUpdate => _animationUpdate.Validated();

        private FanlightTempoSettings Tempo => _tempo.Validated();

        public FanlightLayoutAsset LayoutAsset => _layoutAsset;

        internal bool HasResolvedStateOverride => _hasExternalResolvedStateOverride || _scheduledContributions.Count > 0;

        public FanlightResolvedIntent BaseIntent => FanlightLegacyIntentAdapter.ToIntent(
            _motionPreset != null ? _motionPreset.Settings : _motion,
            _colorPreset != null ? _colorPreset.Settings : _color,
            _audienceSettings);

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

            EnsureShowPipeline();
        }

        private void LateUpdate()
        {
            if (!Enable) return;

            EnsureShowPipeline();

            if (_timeCoordinator == null)
            {
                Dispose();

                return;
            }

            var tempo = Tempo;

            _timeCoordinator.ConfigureCompatibilityTempo(
                tempo.bpm,
                tempo.beatsPerBar,
                tempo.offsetSeconds - tempo.latencyCompensationSeconds);

            var evaluationId = Application.isPlaying
                ? UnityEngine.Time.frameCount
                : (long)Math.Floor(UnityEngine.Time.realtimeSinceStartupAsDouble * 1000d);

            if (!_timeCoordinator.TrySample(evaluationId, out var time, out _))
            {
                Dispose();

                return;
            }

            var template = CreateLegacyTemplate(time);
            var snapshot = CreateSnapshot(template);

            var showSample = _showSession.Evaluate(
                time,
                snapshot,
                _showEvaluator,
                new FanlightEvaluationOptions(
                    AnimationUpdate.Mode == FanlightGpuUpdateMode.FixedRate ? AnimationUpdate.TargetFrameRate : 0d,
                    1e-6d,
                    0.5f,
                    FanlightColorBlendSpace.LinearRgb));

            var state = FanlightLegacyIntentAdapter.ToLegacyState(showSample, template);

            Render(state, state.IsTimeJump || _externalOverrideTimeJumpPending);

            _externalOverrideTimeJumpPending = false;
        }

        private void OnDisable()
        {
            ClearScheduledContributions();
            ClearResolvedStateOverride();
            Dispose();
        }

        private void OnDestroy()
        {
            ClearScheduledContributions();
            ClearResolvedStateOverride();
            Dispose();
        }

        private void OnValidate()
        {
            _validatedSeatLayout = null;
            _legacyRuntimeLayout = null;
            _assetRuntimeLayout = null;
#if UNITY_EDITOR
            _editorPreviewLayout = null;
            _editorLayoutBlocked = false;
#endif
            _color = _color.Validated();
            _showVersion = Math.Max(1, _showVersion);

            if (string.IsNullOrWhiteSpace(_sessionId)) _sessionId = Guid.NewGuid().ToString("N");

#if UNITY_EDITOR
            foreach (var other in FindObjectsByType<PrismFanlight>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other == this) continue;
                if (string.Equals(other._sessionId, _sessionId, StringComparison.Ordinal)) _sessionId = Guid.NewGuid().ToString("N");
            }
#endif

            if (_timeCoordinator == null) _timeCoordinator = GetComponent<ShowTimeCoordinatorBehaviour>();

            _showSession = null;
        }

        private void Render(FanlightResolvedState state, bool isTimeJump)
        {
            if (!Enable) return;

            var cameraPosition = _cullingCamera != null ? _cullingCamera.transform.position : transform.position;

            var runtimeLayout = GetRuntimeLayout();
            if (runtimeLayout == null)
            {
                _renderer.Dispose();
                return;
            }

            _renderer.Render(
                _mesh,
                _penlightAppearanceProfile,
                _material,
                _computeShader,
                _renderingLayerMask,
                _cullingCamera,
                IsCullingEnabled,
                VisibilityUpdate,
                AnimationUpdate,
                runtimeLayout,
                _audienceMaterial,
                state,
                isTimeJump,
                cameraPosition);
        }

        private void Dispose()
        {
            _renderer.Dispose();
        }


        public SeatLayout GetSeatLayout() => GetValidatedSeatLayout();

        public FanlightMotionSettings GetMotionSettings() => (_motionPreset != null ? _motionPreset.Settings : _motion).Validated();

        public FanlightColorSettings GetColorSettings() => (_colorPreset != null ? _colorPreset.Settings : _color).Validated();

        public void SetScheduledContribution(object sourceToken, in FanlightContribution contribution)
        {
            if (sourceToken == null) throw new ArgumentNullException(nameof(sourceToken));
            if (contribution.Layer != FanlightContributionLayer.Scheduled)
                throw new ArgumentException("Timeline and scheduled adapters must submit Scheduled contributions.", nameof(contribution));

            EnsureShowPipeline();

            if (!_scheduledContributions.TryGetValue(sourceToken, out var source))
            {
                source = new FanlightSingleContributionSource(contribution);
                _scheduledContributions.Add(sourceToken, source);
                _showSession.RegisterSource(source);
            }
            else
            {
                source.Set(contribution);
            }

            ResolvedStateOverrideChanged?.Invoke(this);
        }

        public void ClearScheduledContribution(object sourceToken)
        {
            if (sourceToken == null || !_scheduledContributions.TryGetValue(sourceToken, out var source)) return;

            _showSession?.UnregisterSource(source);
            _scheduledContributions.Remove(sourceToken);

            ResolvedStateOverrideChanged?.Invoke(this);
        }

        private void ClearScheduledContributions()
        {
            if (_scheduledContributions.Count == 0) return;
            if (_showSession != null)
            {
                foreach (var source in _scheduledContributions.Values) _showSession.UnregisterSource(source);
            }

            _scheduledContributions.Clear();
            ResolvedStateOverrideChanged?.Invoke(this);
        }

        internal void ClearScheduledContributionsBySourcePrefix(string sourcePrefix)
        {
            if (string.IsNullOrEmpty(sourcePrefix) || _scheduledContributions.Count == 0) return;
            var tokens = new List<object>();
            foreach (var pair in _scheduledContributions)
            {
                if (pair.Value.SourceId.StartsWith(sourcePrefix, StringComparison.Ordinal)) tokens.Add(pair.Key);
            }

            for (var i = 0; i < tokens.Count; i++)
            {
                if (!_scheduledContributions.TryGetValue(tokens[i], out var source)) continue;
                _showSession?.UnregisterSource(source);
                _scheduledContributions.Remove(tokens[i]);
            }

            if (tokens.Count > 0) ResolvedStateOverrideChanged?.Invoke(this);
        }

        private void ClearResolvedStateOverride()
        {
            if (!_hasExternalResolvedStateOverride) return;

            _hasExternalResolvedStateOverride = false;
            _externalOverrideTimeJumpPending = false;
            if (_externalContribution != null)
            {
                _showSession?.UnregisterSource(_externalContribution);
                _externalContribution = null;
            }

            ResolvedStateOverrideChanged?.Invoke(this);
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
#if UNITY_EDITOR
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
#endif
        }

        internal ulong EditorPreviewContentHash
        {
            get
            {
#if UNITY_EDITOR
                return _editorPreviewLayout?.ContentHash ?? 0UL;
#else
                return 0UL;
#endif
            }
        }

        internal void SetEditorLayoutBlocked(bool blocked)
        {
#if UNITY_EDITOR
            if (_editorLayoutBlocked == blocked) return;
            _editorLayoutBlocked = blocked;
            if (blocked)
            {
                _editorPreviewLayout = null;
                Dispose();
            }
#endif
        }

        internal void ClearEditorLayoutPreview()
        {
#if UNITY_EDITOR
            _editorPreviewLayout = null;
            _editorLayoutBlocked = false;
            _assetRuntimeLayout = null;
            Dispose();
#endif
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

        private void EnsureShowPipeline()
        {
            if (_timeCoordinator == null) _timeCoordinator = GetComponent<ShowTimeCoordinatorBehaviour>();
            if (_timeCoordinator == null) _timeCoordinator = gameObject.AddComponent<ShowTimeCoordinatorBehaviour>();

            _timeCoordinator.ConfigureCompatibilityIdentity(
                string.IsNullOrWhiteSpace(_sessionId) ? $"time:{_showId}" : $"time:{_sessionId}");

            if (_showSession != null) return;

            _showSession = new FanlightShowSession(
                string.IsNullOrEmpty(_showId) ? "show.compatibility" : _showId,
                string.IsNullOrWhiteSpace(_sessionId) ? $"session:{_showId}" : _sessionId);

            foreach (var source in _scheduledContributions.Values) _showSession.RegisterSource(source);

            if (_externalContribution != null) _showSession.RegisterSource(_externalContribution);
        }

        private FanlightResolvedState CreateLegacyTemplate(in FanlightShowTimeSample time)
        {
            return new FanlightResolvedState(
                FanlightTempoState.FromMusicalPosition(
                    time.Status is FanlightClockStatus.Ready or FanlightClockStatus.Holding,
                    time.MusicalPosition),
                _motionPreset != null ? _motionPreset.Settings : _motion,
                _colorPreset != null ? _colorPreset.Settings : _color,
                _audienceSettings,
                _lod,
                _random,
                _swingTarget != null ? _swingTarget.position : Vector3.zero,
                transform.localToWorldMatrix,
                (float)time.Seconds,
                (float)time.Seconds,
                time.Discontinuity != FanlightTimeDiscontinuity.None);
        }

        private FanlightShowSnapshot CreateSnapshot(in FanlightResolvedState template)
        {
            var layoutId = _layoutAsset != null && _layoutAsset.LayoutId.IsValid
                ? _layoutAsset.LayoutId.Value
                : "layout.legacy";
            var layoutVersion = _layoutAsset != null ? _layoutAsset.LayoutVersion : 1;
            return new FanlightShowSnapshot(
                string.IsNullOrEmpty(_showId) ? "show.compatibility" : _showId,
                Math.Max(1, _showVersion),
                layoutId,
                Math.Max(1, layoutVersion),
                "persona.compatibility",
                1,
                "gesture.compatibility",
                1,
                "cue.compatibility",
                1,
                template.Random.globalSeed,
                FanlightLegacyIntentAdapter.ToIntent(template.Motion, template.Color, template.Audience));
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
                    || (_layoutAsset.ActiveBake != null && _assetRuntimeLayout.ContentHash != _layoutAsset.ActiveBake.ContentHash))
                {
                    _assetRuntimeLayout = FanlightRuntimeLayout.FromArtifact(_layoutAsset);
                }

                return _assetRuntimeLayout;
            }

            return _legacyRuntimeLayout ??= FanlightRuntimeLayout.FromLegacy(GetValidatedSeatLayout());
        }
    }
}
