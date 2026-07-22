using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal struct FanlightMotionSample
    {
        // Fields

        internal const int Stride = sizeof(float) * 8;

        [SerializeField]
        private Vector4 _armDirectionExtension;

        [SerializeField]
        private Vector4 _penlightDirectionBodyLean;


        // Properties

        internal Vector4 ArmDirectionExtension => _armDirectionExtension;

        internal Vector4 PenlightDirectionBodyLean => _penlightDirectionBodyLean;

        internal Vector3 ArmDirection => new(
            _armDirectionExtension.x,
            _armDirectionExtension.y,
            _armDirectionExtension.z);

        internal float ArmExtension => _armDirectionExtension.w;

        internal Vector3 PenlightDirection => new(
            _penlightDirectionBodyLean.x,
            _penlightDirectionBodyLean.y,
            _penlightDirectionBodyLean.z);

        internal float BodyLean => _penlightDirectionBodyLean.w;

        internal bool IsValid => IsFinite(_armDirectionExtension)
                                 && IsFinite(_penlightDirectionBodyLean)
                                 && ArmDirection.sqrMagnitude > 0.000001f
                                 && PenlightDirection.sqrMagnitude > 0.000001f;


        // Methods

        internal FanlightMotionSample(
            Vector3 armDirection,
            float armExtension,
            Vector3 penlightDirection,
            float bodyLeanRadians)
        {
            armDirection = NormalizeDirection(armDirection, Vector3.forward);
            penlightDirection = NormalizeDirection(penlightDirection, Vector3.up);
            _armDirectionExtension = new Vector4(
                armDirection.x,
                armDirection.y,
                armDirection.z,
                armExtension);
            _penlightDirectionBodyLean = new Vector4(
                penlightDirection.x,
                penlightDirection.y,
                penlightDirection.z,
                bodyLeanRadians);
        }

        private static Vector3 NormalizeDirection(Vector3 direction, Vector3 fallback)
        {
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f) return fallback;
            return direction.normalized;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static bool IsFinite(Vector4 value) =>
            float.IsFinite(value.x)
            && float.IsFinite(value.y)
            && float.IsFinite(value.z)
            && float.IsFinite(value.w);
    }
}
