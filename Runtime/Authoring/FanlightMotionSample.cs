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
        private Vector4 _arm;

        [SerializeField]
        private Vector4 _penlight;


        // Properties

        internal Vector4 Arm => _arm;

        internal Vector4 Penlight => _penlight;


        // Methods

        internal FanlightMotionSample(
            float armElevationRadians,
            float armSideRadians,
            float armExtension,
            float bodyLeanRadians,
            float penlightElevationRadians,
            float penlightSideRadians)
        {
            _arm = new Vector4(
                armElevationRadians,
                armSideRadians,
                armExtension,
                bodyLeanRadians);
            _penlight = new Vector4(
                penlightElevationRadians,
                penlightSideRadians,
                0f,
                0f);
        }
    }
}
