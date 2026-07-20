using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuUpdateScheduler
    {
        private UpdateLane _visibility;
        private UpdateLane _animation;


        internal void Reset()
        {
            _visibility.Reset();
            _animation.Reset();
        }

        internal bool ShouldUpdateVisibility(FanlightGpuUpdateTiming timing, float clock)
        {
            return ShouldUpdate(ref _visibility, timing.Validated(), clock);
        }

        internal bool ShouldUpdateAnimation(FanlightGpuUpdateTiming timing, float clock, bool force)
        {
            if (force)
            {
                _animation.MarkUpdated(clock);
                return true;
            }

            return ShouldUpdate(ref _animation, timing.Validated(), clock);
        }

        private static bool ShouldUpdate(ref UpdateLane lane, FanlightGpuUpdateTiming timing, float clock)
        {
            if (timing.Mode == FanlightGpuUpdateMode.EveryFrame)
            {
                lane.MarkUpdated(clock);
                return true;
            }

            var interval = GetInterval(timing);

            if (!lane.HasUpdated || Mathf.Abs(clock - lane.LastUpdateTime) >= interval)
            {
                lane.MarkUpdated(clock);
                return true;
            }

            return false;
        }

        private static float GetInterval(FanlightGpuUpdateTiming timing)
        {
            return timing.Mode == FanlightGpuUpdateMode.EveryFrame ? 0.0f : 1.0f / timing.TargetFrameRate;
        }


        private struct UpdateLane
        {
            internal bool HasUpdated { get; private set; }

            internal float LastUpdateTime { get; private set; }


            internal void MarkUpdated(float clock)
            {
                HasUpdated = true;
                LastUpdateTime = clock;
            }

            internal void Reset()
            {
                HasUpdated = false;
                LastUpdateTime = 0.0f;
            }
        }
    }
}
