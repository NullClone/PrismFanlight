using PrismFanlight.Live;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrismFanlight.Samples
{
    public sealed class FanlightInputBinding : MonoBehaviour
    {
        // Fields

        [SerializeField] private FanlightLiveControl _control;
        [SerializeField] private InputActionReference _action;
        [SerializeField] private FanlightInputTarget _target;
        [SerializeField, Min(0)] private int _lookIndex;
        [SerializeField] private FanlightLiveParameter _parameter;
        [SerializeField] private float _minimum;
        [SerializeField] private float _maximum = 1f;


        // Methods

        private void OnEnable()
        {
            if (_action == null || _action.action == null) return;

            _action.action.performed += OnPerformed;
            _action.action.Enable();
        }

        private void OnDisable()
        {
            if (_action == null || _action.action == null) return;

            _action.action.performed -= OnPerformed;
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            if (_control == null) return;

            switch (_target)
            {
                case FanlightInputTarget.Look:
                    _control.RequestLookAt(_lookIndex);
                    break;

                case FanlightInputTarget.ToggleBlackout:
                    _control.ToggleBlackout();
                    break;

                case FanlightInputTarget.MasterIntensity:
                    _control.SetMasterIntensity(context.ReadValue<float>());
                    break;

                case FanlightInputTarget.Parameter:
                    _control.SetParameter(_parameter, Mathf.Lerp(_minimum, _maximum, context.ReadValue<float>()));
                    break;
            }
        }
    }
}
