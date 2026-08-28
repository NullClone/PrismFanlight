using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal sealed class FanlightLayoutRow
    {
        // Fields

        [SerializeField]
        private Vector3 _leftPoint;

        [SerializeField]
        private Vector3 _controlPoint;

        [SerializeField]
        private Vector3 _rightPoint;

        [SerializeField]
        private ulong[] _stableSeatIds;


        // Properties

        internal Vector3 LeftPoint => _leftPoint;

        internal Vector3 ControlPoint => _controlPoint;

        internal Vector3 RightPoint => _rightPoint;

        internal int SeatCount => _stableSeatIds?.Length ?? 0;


        // Methods

        internal FanlightLayoutRow(
            Vector3 leftPoint,
            Vector3 controlPoint,
            Vector3 rightPoint,
            ulong[] stableSeatIds)
        {
            _leftPoint = leftPoint;
            _controlPoint = controlPoint;
            _rightPoint = rightPoint;
            _stableSeatIds = stableSeatIds == null ? Array.Empty<ulong>() : (ulong[])stableSeatIds.Clone();
        }

        internal ulong GetStableSeatId(int seatIndex) => _stableSeatIds[seatIndex];

        internal ulong[] CopyStableSeatIds()
            => _stableSeatIds == null ? Array.Empty<ulong>() : (ulong[])_stableSeatIds.Clone();

        internal void SetGeometry(Vector3 leftPoint, Vector3 controlPoint, Vector3 rightPoint)
        {
            _leftPoint = leftPoint;
            _controlPoint = controlPoint;
            _rightPoint = rightPoint;
        }

        internal void SetStableSeatIds(ulong[] stableSeatIds)
        {
            _stableSeatIds = stableSeatIds == null ? Array.Empty<ulong>() : (ulong[])stableSeatIds.Clone();
        }
    }
}
