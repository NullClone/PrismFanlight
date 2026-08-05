using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using PrismFanlight.Rendering;
using PrismFanlight.Time;
using UnityEngine;

namespace PrismFanlight
{
    [ExecuteAlways]
    [HelpURL(HelpUrl)]
    [AddComponentMenu("Prism Fanlight/Prism Fanlight")]
    public sealed class PrismFanlight : MonoBehaviour
    {
        // Fields

        public const string HelpUrl = ""; //https://github.com/NullClone/PrismFanlight


        [SerializeField]
        private Material _material;

        [SerializeField]
        private Material _audienceMaterial;

        [SerializeField]
        private ComputeShader _computeShader;

        [SerializeField]
        private uint _renderingLayerMask = 1u;

        [SerializeField]
        private bool _enableCulling;

        [SerializeField]
        private Camera _cullingCamera;

        [SerializeField]
        private FanlightGpuUpdateTiming _updateMode = FanlightGpuUpdateTiming.FixedRate(60f);

        [SerializeField]
        private FanlightPenlightAsset _penlightAppearanceProfile;

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
        private Dictionary<object, FanlightTempoCandidate> _scheduledTempoCandidates;
        private Dictionary<object, Action> _scheduledTimelineReleases;
        private FanlightContributionBuffer _contributionBuffer;
        private FanlightShowEvaluator _showEvaluator;
        private FanlightTempoScopeResolver _tempoScopeResolver;
        private FanlightTempoCandidate[] _tempoCandidateSnapshot;
        private FanlightTimeManager _tempoScopeManager;
        private int _tempoScopeRevision = int.MinValue;
        private FanlightShowTimeFault _timeFault;
        private string _sequenceFault = string.Empty;
        private long _evaluationId;
        private long _renderFrameId;
        private FanlightShowSample _heldTimelineSample;
        private bool _hasHeldTimelineState;
        private bool _timelineEvaluatedSinceLastUpdate;
        private bool _timelineFaultReportedSinceLastUpdate;
        private string _reportedTimelineFault = string.Empty;
        private bool _baseStateValid;
        private string _baseStateFault = string.Empty;
        private FanlightRuntimeLayout _assetRuntimeLayout;


#if UNITY_EDITOR
        private FanlightRuntimeLayout _editorPreviewLayout;
        private bool _editorLayoutBlocked;
#endif


        // Properties

        internal FanlightLayoutAsset LayoutAsset => _layoutAsset;

        private FanlightGpuUpdateTiming AnimationUpdate => _updateMode.Validated();

        internal bool IsReady => _renderer is { IsReady: true };

        internal FanlightRendererFault RendererFault => _renderer?.Fault ?? FanlightRendererFault.MissingResource;

        internal FanlightShowTimeFault TimeFault => _timeFault;

        internal string SequenceFault => _sequenceFault;

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
            var defaultMotionAsset = Resources.Load<FanlightMotionAsset>("Default Motion Drum Asset");
            if (defaultMotionAsset != null)
            {
                _motion = FanlightShowStateDefaults.Motion(defaultMotionAsset);
            }
        }
#endif

        private void OnEnable()
        {
            EnsureRuntimeState();
            ValidateBaseState();
        }

        private void LateUpdate()
        {
            if (!enabled || !SystemInfo.supportsComputeShaders)
            {
                ClearScheduledTempoCandidates();
                ClearScheduledContributions();
                ReleaseScheduledTimelineResources();
                ClearHeldTimelineState();
                Dispose();
                return;
            }

            if (_timeManager == null || _evaluationId == long.MaxValue)
            {
                ClearScheduledTempoCandidates();
                ClearScheduledContributions();
                ReleaseScheduledTimelineResources();
                ClearHeldTimelineState();
                Dispose();
                return;
            }

            EnsureRuntimeState();

            if (!_baseStateValid)
            {
                StopForSequenceFault(_baseStateFault);
                return;
            }

            _evaluationId++;

            if (!_timeManager.TrySampleClock(_evaluationId, out var clock, out _timeFault))
            {
                ClearScheduledTempoCandidates();
                ClearScheduledContributions();
                ReleaseScheduledTimelineResources();
                ClearHeldTimelineState();
                Dispose();
                return;
            }

            EnsureTempoScopeResolver();
            var timelineEvaluated = ConsumeTimelineEvaluationFlag();
            var tempoCandidateCount = SnapshotAndClearTempoCandidates();

            if (TryConsumeReportedTimelineFault(out var timelineFault))
            {
                StopForSequenceFault(timelineFault);
                return;
            }

            if (!timelineEvaluated
                && clock.Status == FanlightClockStatus.Holding
                && _hasHeldTimelineState)
            {
                ClearScheduledContributions();
                _timeFault = FanlightShowTimeFault.None;
                _sequenceFault = string.Empty;
                RenderFrame(_heldTimelineSample);
                ReleaseScheduledTimelineResources();
                return;
            }

            if (!_tempoScopeResolver.TryResolve(
                    clock,
                    _tempoCandidateSnapshot.AsSpan(0, tempoCandidateCount),
                    out var time,
                    out _timeFault))
            {
                ClearScheduledContributions();
                ReleaseScheduledTimelineResources();
                ClearHeldTimelineState();
                Dispose();
                return;
            }

            _contributionBuffer.Clear();

            foreach (var contribution in _scheduledContributions.Values)
            {
                _contributionBuffer.Add(contribution);
            }

            _scheduledContributions.Clear();

            var options = new FanlightEvaluationOptions(AnimationUpdate.Mode == FanlightGpuUpdateMode.FixedRate ? AnimationUpdate.TargetFrameRate : 0d, 1e-6d);
            var request = new FanlightShowEvaluationRequest(time, BaseState, _contributionBuffer.AsMemory(), options);
            FanlightShowSample sample;

            try
            {
                sample = _showEvaluator.Evaluate(request);
                _sequenceFault = string.Empty;
            }
            catch (InvalidOperationException exception)
            {
                StopForSequenceFault(exception.Message);
                return;
            }
            catch (ArgumentException exception)
            {
                StopForSequenceFault(exception.Message);
                return;
            }

            if (timelineEvaluated)
            {
                _heldTimelineSample = sample;
                _hasHeldTimelineState = true;
            }
            else if (clock.Status != FanlightClockStatus.Holding)
            {
                ClearHeldTimelineState();
            }

            _timeFault = FanlightShowTimeFault.None;
            RenderFrame(sample);
            ReleaseScheduledTimelineResources();
        }

        private void OnDisable()
        {
            ClearScheduledTempoCandidates();
            ClearScheduledContributions();
            ReleaseScheduledTimelineResources();
            ClearHeldTimelineState();
            Dispose();
        }

        private void OnDestroy()
        {
            ClearScheduledTempoCandidates();
            ClearScheduledContributions();
            ReleaseScheduledTimelineResources();
            ClearHeldTimelineState();
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
            ValidateBaseState();
            _tempoScopeResolver = null;
            _tempoScopeManager = null;
            _tempoScopeRevision = int.MinValue;
            ClearHeldTimelineState();
        }

        internal void SetScheduledTempoCandidate(object sourceToken, in FanlightTempoCandidate candidate)
        {
            if (sourceToken == null)
            {
                throw new ArgumentNullException(nameof(sourceToken));
            }

            EnsureRuntimeState();
            _scheduledTempoCandidates[sourceToken] = candidate;
            _timelineEvaluatedSinceLastUpdate = true;
        }

        internal void ClearScheduledTempoCandidate(object sourceToken)
        {
            if (sourceToken != null) _scheduledTempoCandidates?.Remove(sourceToken);
        }

        internal void SetScheduledContribution(object sourceToken, in FanlightShowContribution contribution)
        {
            if (sourceToken == null)
            {
                throw new ArgumentNullException(nameof(sourceToken));
            }

            EnsureRuntimeState();
            _scheduledContributions[sourceToken] = contribution;
            _timelineEvaluatedSinceLastUpdate = true;
        }

        internal void ClearScheduledContribution(object sourceToken)
        {
            if (sourceToken != null) _scheduledContributions?.Remove(sourceToken);
        }

        internal void MarkScheduledTimelineEvaluation()
        {
            _timelineEvaluatedSinceLastUpdate = true;
        }

        internal void ReportTimelineFault(string fault)
        {
            if (!_timelineFaultReportedSinceLastUpdate)
            {
                _reportedTimelineFault = string.IsNullOrEmpty(fault)
                    ? "Timeline evaluation contains an invalid value."
                    : fault;
            }

            _timelineFaultReportedSinceLastUpdate = true;
            _timelineEvaluatedSinceLastUpdate = true;
        }

        internal void ClearHeldTimelineState()
        {
            _hasHeldTimelineState = false;
            _heldTimelineSample = default;
        }

        internal void ScheduleTimelineRelease(object sourceToken, Action release)
        {
            if (sourceToken == null)
            {
                throw new ArgumentNullException(nameof(sourceToken));
            }

            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }

            if (!isActiveAndEnabled)
            {
                release();
                return;
            }

            EnsureRuntimeState();
            _scheduledTimelineReleases[sourceToken] = release;
        }

        internal void CancelTimelineRelease(object sourceToken)
        {
            if (sourceToken != null) _scheduledTimelineReleases?.Remove(sourceToken);
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
            if (_timeManager != timeManager) ClearHeldTimelineState();
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

        private void RenderFrame(in FanlightShowSample sample)
        {
            EnsureRuntimeState();

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

            _renderFrameId = _renderFrameId == long.MaxValue ? 1L : _renderFrameId + 1L;
            var frame = new FanlightFrameContext(
                _renderFrameId,
                transform.localToWorldMatrix,
                _swingTarget != null ? _swingTarget.position : Vector3.zero);

            _renderer.Render(
                sample,
                frame,
                _cullingCamera,
                _enableCulling && _cullingCamera != null,
                gameObject.layer,
                _renderingLayerMask,
                AnimationUpdate);
        }

        private void ClearScheduledContributions()
        {
            _scheduledContributions?.Clear();
            _contributionBuffer?.Clear();
            _timelineEvaluatedSinceLastUpdate = false;
            _timelineFaultReportedSinceLastUpdate = false;
            _reportedTimelineFault = string.Empty;
        }

        private void ClearScheduledTempoCandidates()
        {
            _scheduledTempoCandidates?.Clear();

            if (_tempoCandidateSnapshot is { Length: > 0 })
            {
                Array.Clear(_tempoCandidateSnapshot, 0, _tempoCandidateSnapshot.Length);
            }
        }

        private void Dispose()
        {
            _renderer?.Dispose();
        }

        private void EnsureRuntimeState()
        {
            _renderer ??= new FanlightGpuRenderer();
            _scheduledContributions ??= new Dictionary<object, FanlightShowContribution>();
            _scheduledTempoCandidates ??= new Dictionary<object, FanlightTempoCandidate>();
            _scheduledTimelineReleases ??= new Dictionary<object, Action>();
            _tempoCandidateSnapshot ??= Array.Empty<FanlightTempoCandidate>();
            _contributionBuffer ??= new FanlightContributionBuffer(16);
            _showEvaluator ??= new FanlightShowEvaluator();
        }

        private void ValidateBaseState()
        {
            try
            {
                _ = FanlightShowStatePatcher.Validate(BaseState);
                _baseStateValid = true;
                _baseStateFault = string.Empty;
            }
            catch (ArgumentException exception)
            {
                _baseStateValid = false;
                _baseStateFault = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                _baseStateValid = false;
                _baseStateFault = exception.Message;
            }
        }

        private void EnsureTempoScopeResolver()
        {
            if (_tempoScopeResolver != null
                && _tempoScopeManager == _timeManager
                && _tempoScopeRevision == _timeManager.DefaultTempoRevision)
            {
                return;
            }

            _tempoScopeResolver = new FanlightTempoScopeResolver(
                _timeManager.DefaultBpm,
                _timeManager.DefaultBeatsPerBar,
                _timeManager.DefaultBeatUnit,
                _timeManager.DefaultMusicalOriginSeconds);
            _tempoScopeManager = _timeManager;
            _tempoScopeRevision = _timeManager.DefaultTempoRevision;
        }

        private int SnapshotAndClearTempoCandidates()
        {
            var count = _scheduledTempoCandidates.Count;

            if (_tempoCandidateSnapshot.Length < count)
            {
                Array.Resize(ref _tempoCandidateSnapshot, count);
            }

            var index = 0;

            foreach (var candidate in _scheduledTempoCandidates.Values)
            {
                _tempoCandidateSnapshot[index++] = candidate;
            }

            _scheduledTempoCandidates.Clear();
            return count;
        }

        private bool ConsumeTimelineEvaluationFlag()
        {
            var value = _timelineEvaluatedSinceLastUpdate;
            _timelineEvaluatedSinceLastUpdate = false;
            return value;
        }

        private bool TryConsumeReportedTimelineFault(out string fault)
        {
            fault = _reportedTimelineFault;
            var reported = _timelineFaultReportedSinceLastUpdate;
            _timelineFaultReportedSinceLastUpdate = false;
            _reportedTimelineFault = string.Empty;
            return reported;
        }

        private void StopForSequenceFault(string fault)
        {
            _sequenceFault = string.IsNullOrEmpty(fault)
                ? "Timeline evaluation contains an invalid value."
                : fault;
            ClearScheduledTempoCandidates();
            ClearScheduledContributions();
            ReleaseScheduledTimelineResources();
            ClearHeldTimelineState();
            Dispose();
        }

        private void ReleaseScheduledTimelineResources()
        {
            if (_scheduledTimelineReleases == null || _scheduledTimelineReleases.Count == 0) return;

            foreach (var release in _scheduledTimelineReleases.Values)
            {
                release();
            }

            _scheduledTimelineReleases.Clear();
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
