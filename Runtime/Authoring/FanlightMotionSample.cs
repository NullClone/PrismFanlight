using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal struct FanlightMotionSample
    {
        // Fields

        internal const int Stride = sizeof(float) * 16;


        [SerializeField]
        private Vector4 _bodyPosition;

        [SerializeField]
        private Vector4 _bodyRotation;

        [SerializeField]
        private Vector4 _handPosition;

        [SerializeField]
        private Vector4 _penlightRotation;


        // Properties

        internal Vector4 BodyPositionData => _bodyPosition;

        internal Vector4 BodyRotationData => _bodyRotation;

        internal Vector4 HandPositionData => _handPosition;

        internal Vector4 PenlightRotationData => _penlightRotation;

        internal Vector3 BodyPosition => new(_bodyPosition.x, _bodyPosition.y, _bodyPosition.z);

        internal Quaternion BodyRotation => new(
            _bodyRotation.x,
            _bodyRotation.y,
            _bodyRotation.z,
            _bodyRotation.w);

        internal Vector3 HandPosition => new(_handPosition.x, _handPosition.y, _handPosition.z);

        internal Quaternion PenlightRotation => new(
            _penlightRotation.x,
            _penlightRotation.y,
            _penlightRotation.z,
            _penlightRotation.w);

        internal bool IsValid => IsFinite(_bodyPosition)
                                 && IsUnitQuaternion(_bodyRotation)
                                 && IsFinite(_handPosition)
                                 && IsUnitQuaternion(_penlightRotation);


        // Methods

        internal FanlightMotionSample(
            Vector3 bodyPosition,
            Quaternion bodyRotation,
            Vector3 handPosition,
            Quaternion penlightRotation)
        {
            bodyRotation = NormalizeRotation(bodyRotation);
            penlightRotation = NormalizeRotation(penlightRotation);

            _bodyPosition = new Vector4(bodyPosition.x, bodyPosition.y, bodyPosition.z, 0f);
            _bodyRotation = new Vector4(bodyRotation.x, bodyRotation.y, bodyRotation.z, bodyRotation.w);
            _handPosition = new Vector4(handPosition.x, handPosition.y, handPosition.z, 0f);
            _penlightRotation = new Vector4(
                penlightRotation.x,
                penlightRotation.y,
                penlightRotation.z,
                penlightRotation.w);
        }

        internal static FanlightMotionSample Interpolate(
            FanlightMotionSample from,
            FanlightMotionSample to,
            float weight)
        {
            weight = Mathf.Clamp01(weight);

            return new FanlightMotionSample(
                Vector3.LerpUnclamped(from.BodyPosition, to.BodyPosition, weight),
                InterpolateRotation(from.BodyRotation, to.BodyRotation, weight),
                Vector3.LerpUnclamped(from.HandPosition, to.HandPosition, weight),
                InterpolateRotation(from.PenlightRotation, to.PenlightRotation, weight));
        }

        internal static Quaternion InterpolateRotation(Quaternion from, Quaternion to, float weight)
        {
            from = NormalizeRotation(from);
            to = NormalizeRotation(to);

            if (Quaternion.Dot(from, to) < 0f)
            {
                to = new Quaternion(-to.x, -to.y, -to.z, -to.w);
            }

            return NormalizeRotation(new Quaternion(
                Mathf.LerpUnclamped(from.x, to.x, weight),
                Mathf.LerpUnclamped(from.y, to.y, weight),
                Mathf.LerpUnclamped(from.z, to.z, weight),
                Mathf.LerpUnclamped(from.w, to.w, weight)));
        }

        internal static Quaternion NormalizeRotation(Quaternion rotation)
        {
            var magnitude = Mathf.Sqrt(
                rotation.x * rotation.x
                + rotation.y * rotation.y
                + rotation.z * rotation.z
                + rotation.w * rotation.w);

            if (!float.IsFinite(magnitude) || magnitude <= 0.000001f) return Quaternion.identity;

            var inverse = 1f / magnitude;
            return new Quaternion(
                rotation.x * inverse,
                rotation.y * inverse,
                rotation.z * inverse,
                rotation.w * inverse);
        }

        private static bool IsUnitQuaternion(Vector4 value)
        {
            if (!IsFinite(value)) return false;

            var squareMagnitude = Vector4.Dot(value, value);

            return squareMagnitude >= 0.999f && squareMagnitude <= 1.001f;
        }

        private static bool IsFinite(Vector4 value) =>
            float.IsFinite(value.x)
            && float.IsFinite(value.y)
            && float.IsFinite(value.z)
            && float.IsFinite(value.w);
    }
}
