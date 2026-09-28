using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Live
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrismFanlight))]
    [HelpURL(PrismFanlight.HelpUrl)]
    [AddComponentMenu("Rendering/Prism Fanlight Live Control")]
    public sealed class FanlightLiveControl : MonoBehaviour
    {
        // Fields

        [SerializeField]
        private List<FanlightLiveLook> _looks = new();


        private readonly float[] _parameterValues = new float[(int)FanlightLiveParameter.Count];
        private PrismFanlight _fanlight;
        private FanlightLiveLookQueue _queue;
        private int _pendingParameters;


        // Properties

        internal IReadOnlyList<FanlightLiveLook> Looks => _looks;

        internal FanlightLiveLook PendingLook => _queue?.PendingLook;

        internal FanlightLiveQuantize PendingQuantize => _queue?.PendingQuantize ?? FanlightLiveQuantize.Immediate;

        internal FanlightLiveLook LastAppliedLook { get; private set; }

        internal string Fault { get; private set; } = string.Empty;

        internal float MasterIntensity { get; set; } = 1f;

        internal bool Blackout { get; set; }

        internal float OutputIntensityScale => Blackout ? 0f : MasterIntensity;

        public FanlightLiveStatus Status { get; private set; }


        // Methods

        private void OnEnable()
        {
            _queue ??= new FanlightLiveLookQueue();
            _fanlight = GetComponent<PrismFanlight>();

            if (_fanlight != null) _fanlight.RegisterLiveControl(this);
        }

        private void OnDisable()
        {
            _queue?.Clear();
            _pendingParameters = 0;
            Status = default;

            if (_fanlight != null) _fanlight.UnregisterLiveControl(this);

            _fanlight = null;
        }

        public void RequestLook(FanlightLiveLook look)
        {
            if (!CanAcceptInput()) return;

            if (look == null)
            {
                Fault = "Look is not assigned.";
                return;
            }

            _queue.Request(look, look.Quantize);

            Fault = string.Empty;
        }

        public void RequestLookAt(int index)
        {
            if (index < 0 || index >= _looks.Count)
            {
                Fault = $"Look index {index} is out of range.";
                return;
            }

            RequestLook(_looks[index]);
        }

        public void CancelPendingLook()
        {
            _queue?.Clear();
        }

        public void SetMasterIntensity(float value)
        {
            if (!CanAcceptInput()) return;

            if (!(value >= 0f && value <= 1f))
            {
                Fault = $"Master intensity {value} must be between 0 and 1.";
                return;
            }

            MasterIntensity = value;
        }

        public void SetBlackout(bool enabled)
        {
            if (!CanAcceptInput()) return;

            Blackout = enabled;
        }

        public void ToggleBlackout()
        {
            if (!CanAcceptInput()) return;

            Blackout = !Blackout;
        }

        public void SetParameter(FanlightLiveParameter parameter, float value)
        {
            if (!CanAcceptInput()) return;

            if (!IsDefined(parameter))
            {
                Fault = $"Live parameter {(int)parameter} is not defined.";
                return;
            }

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                Fault = $"Live parameter {parameter} must be finite.";
                return;
            }

            _parameterValues[(int)parameter] = value;
            _pendingParameters |= 1 << (int)parameter;
        }

        public bool TryGetParameter(FanlightLiveParameter parameter, out float value)
        {
            value = 0f;

            if (!IsDefined(parameter)) return false;

            if (HasPending(_pendingParameters, parameter))
            {
                value = _parameterValues[(int)parameter];
                return true;
            }

            if (_fanlight == null) return false;

            var state = _fanlight.BaseState;

            value = parameter switch
            {
                FanlightLiveParameter.Energy => state.Intent.Energy,
                FanlightLiveParameter.Participation => state.Intent.Participation,
                FanlightLiveParameter.Synchronization => state.Intent.Synchronization,
                FanlightLiveParameter.TransitionScatter => state.Intent.TransitionScatter,
                _ => state.Intensity.BaseIntensity
            };

            return true;
        }


        internal bool TryTakeDueLook(in FanlightShowTimeSample time, out FanlightLiveLook look)
        {
            look = null;
            return _queue != null && _queue.TryTakeDue(time, out look);
        }

        internal bool TryTakeParameterPatch(in FanlightShowState current, out FanlightShowPatch patch)
        {
            patch = default;

            var pending = _pendingParameters;
            if (pending == 0) return false;

            _pendingParameters = 0;

            var intentFields = FanlightIntentFields.None;
            if (HasPending(pending, FanlightLiveParameter.Energy)) intentFields |= FanlightIntentFields.Energy;
            if (HasPending(pending, FanlightLiveParameter.Participation)) intentFields |= FanlightIntentFields.Participation;
            if (HasPending(pending, FanlightLiveParameter.Synchronization)) intentFields |= FanlightIntentFields.Synchronization;
            if (HasPending(pending, FanlightLiveParameter.TransitionScatter)) intentFields |= FanlightIntentFields.TransitionScatter;

            var intent = current.Intent;
            var intentValue = intentFields == FanlightIntentFields.None
                ? intent
                : new FanlightIntentState(
                    ValueOr(pending, FanlightLiveParameter.Energy, intent.Energy),
                    ValueOr(pending, FanlightLiveParameter.Participation, intent.Participation),
                    ValueOr(pending, FanlightLiveParameter.Synchronization, intent.Synchronization),
                    ValueOr(pending, FanlightLiveParameter.TransitionScatter, intent.TransitionScatter));

            var intensity = current.Intensity;
            var intensityFields = HasPending(pending, FanlightLiveParameter.BaseIntensity)
                ? FanlightIntensityFields.BaseIntensity
                : FanlightIntensityFields.None;
            var intensityValue = intensityFields == FanlightIntensityFields.None
                ? intensity
                : FanlightIntensityState.BlendMasks(
                    _parameterValues[(int)FanlightLiveParameter.BaseIntensity],
                    intensity.RandomIntensity,
                    intensity.GetMask(0),
                    intensity.GetMask(1),
                    intensity.GetMask(2),
                    new Vector3(intensity.GetMaskWeight(0), intensity.GetMaskWeight(1), intensity.GetMaskWeight(2)));

            patch = new FanlightShowPatch(
                new FanlightIntentPatch(intentFields, intentValue),
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightIntensityPatch(intensityFields, intensityValue));

            return true;
        }

        internal void ReportApplied(FanlightLiveLook look)
        {
            LastAppliedLook = look;
            Fault = string.Empty;
        }

        internal void ReportFault(string source, string fault)
        {
            Fault = $"{source}: {fault}";
        }

        internal void ReportStatus(in FanlightLiveStatus status)
        {
            Status = status;
        }


        private bool CanAcceptInput()
        {
            if (!Application.isPlaying)
            {
                Fault = "Live input is available only in Play mode.";
                return false;
            }

            if (!isActiveAndEnabled)
            {
                Fault = "Live Control is disabled.";
                return false;
            }

            return true;
        }

        private float ValueOr(int pending, FanlightLiveParameter parameter, float current) => HasPending(pending, parameter) ? _parameterValues[(int)parameter] : current;

        private static bool HasPending(int pending, FanlightLiveParameter parameter) => (pending & (1 << (int)parameter)) != 0;

        private static bool IsDefined(FanlightLiveParameter parameter) => (int)parameter >= 0 && (int)parameter < (int)FanlightLiveParameter.Count;
    }
}
