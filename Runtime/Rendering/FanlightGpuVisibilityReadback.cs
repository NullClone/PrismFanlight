using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuVisibilityReadback
    {
        // Fields

        private readonly Action<AsyncGPUReadbackRequest> _onReadback;
        private int _lastReadbackFrame = -1;
        private bool _readbackPending;
        private int _seatCountForReadback;


        // Properties

        public int VisibleSeatCount { get; private set; }
        public long RequestCount { get; private set; }
        public long LastSuccessfulFrame { get; private set; } = -1;
        public bool IsPending => _readbackPending;


        public FanlightGpuVisibilityReadback()
        {
            _onReadback = OnReadback;
        }


        // Methods

        public void Reset()
        {
            _lastReadbackFrame = -1;
            _readbackPending = false;
            VisibleSeatCount = 0;
            RequestCount = 0;
            LastSuccessfulFrame = -1;
        }

        public void Request(GraphicsBuffer argsBuffer, int seatCount)
        {
            if (argsBuffer == null || _readbackPending) return;
            if (UnityEngine.Time.frameCount == _lastReadbackFrame) return;

            _readbackPending = true;
            _lastReadbackFrame = UnityEngine.Time.frameCount;
            _seatCountForReadback = seatCount;
            RequestCount++;

            AsyncGPUReadback.Request(argsBuffer, _onReadback);
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;

            if (request.hasError) return;

            var args = request.GetData<uint>();
            if (args.Length > 1)
            {
                VisibleSeatCount = (int)math.min(args[1], (uint)_seatCountForReadback);
                LastSuccessfulFrame = UnityEngine.Time.frameCount;
            }
        }
    }
}
