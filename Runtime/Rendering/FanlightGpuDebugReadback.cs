using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuVisibilityReadback
    {
        private const int ReadbackIntervalFrames = 10;

        private int _lastReadbackFrame = -1;
        private bool _readbackPending;


        public int VisibleSeatCount { get; private set; }


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

            AsyncGPUReadback.Request(argsBuffer, request =>
            {
                _readbackPending = false;

                if (request.hasError) return;

                var args = request.GetData<uint>();
                if (args.Length > 1)
                {
                    VisibleSeatCount = (int)math.min(args[1], (uint)seatCount);
                }
            });
        }
    }
}
