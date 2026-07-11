using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuVisibilityReadback
    {
        // Fields

        private const int ReadbackIntervalFrames = 10;

        private readonly Action<AsyncGPUReadbackRequest> _onReadback;
        private int _lastReadbackFrame = -1;
        private bool _readbackPending;
        private int _seatCountForReadback;


        // Properties

        public int VisibleSeatCount { get; private set; }


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
        }

        public void Request(GraphicsBuffer argsBuffer, int seatCount)
        {
            if (argsBuffer == null || _readbackPending) return;
            if (Time.frameCount == _lastReadbackFrame) return;
            if (Time.frameCount % ReadbackIntervalFrames != 0) return;

            _readbackPending = true;
            _lastReadbackFrame = Time.frameCount;
            _seatCountForReadback = seatCount;

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
            }
        }
    }
}
