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
        private int _variantCountForReadback;


        // Properties

        public int VisibleSeatCount { get; private set; }
        public long RequestCount { get; private set; }
        public long LastSuccessfulFrame { get; private set; } = -1;
        public bool IsPending => _readbackPending;

        public int[] VisibleVariantCounts { get; private set; } = Array.Empty<int>();


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
            VisibleVariantCounts = Array.Empty<int>();
        }

        public void Request(GraphicsBuffer argsBuffer, int seatCount, int variantCount)
        {
            if (argsBuffer == null || _readbackPending) return;
            if (UnityEngine.Time.frameCount == _lastReadbackFrame) return;

            _readbackPending = true;
            _lastReadbackFrame = UnityEngine.Time.frameCount;
            _seatCountForReadback = seatCount;
            _variantCountForReadback = Math.Max(1, variantCount);
            RequestCount++;

            AsyncGPUReadback.Request(argsBuffer, _onReadback);
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;

            if (request.hasError) return;

            var args = request.GetData<GraphicsBuffer.IndirectDrawIndexedArgs>();
            if (args.Length > 0)
            {
                var visible = 0u;
                if (VisibleVariantCounts.Length != _variantCountForReadback)
                    VisibleVariantCounts = new int[_variantCountForReadback];
                for (var i = 0; i < _variantCountForReadback; i++)
                {
                    var count = i < args.Length ? args[i].instanceCount : 0u;
                    VisibleVariantCounts[i] = (int)count;
                    visible += count;
                }

                VisibleSeatCount = (int)math.min(visible, (uint)_seatCountForReadback);
                LastSuccessfulFrame = UnityEngine.Time.frameCount;
            }
        }
    }
}
