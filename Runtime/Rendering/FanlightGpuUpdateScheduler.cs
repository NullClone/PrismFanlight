using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightGpuUpdateScheduler
    {
        private UpdateLane _visibility;
        private UpdateLane _animation;


        public void Reset()
        {
            _visibility.Reset();
            _animation.Reset();
        }

        public bool ShouldUpdateVisibility(FanlightGpuUpdateTiming timing, float clock)
        {
            return ShouldUpdate(ref _visibility, timing.Validated(), clock);
        }

        public bool ShouldUpdateAnimation(FanlightGpuUpdateTiming timing, float clock, bool force)
        {
            var validated = timing.Validated();

            if (force)
            {
                _animation.MarkUpdated(clock, GetInterval(validated));
                return true;
            }

            return ShouldUpdate(ref _animation, validated, clock);
        }

        private static bool ShouldUpdate(ref UpdateLane lane, FanlightGpuUpdateTiming timing, float clock)
        {
            if (timing.Mode == FanlightGpuUpdateMode.EveryFrame)
            {
                lane.MarkUpdated(clock, 0.0f);
                return true;
            }

            var interval = GetInterval(timing);

            if (!lane.HasUpdated || clock >= lane.NextUpdateTime)
            {
                lane.MarkUpdated(clock, interval);
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
            public bool HasUpdated { get; private set; }

            public float NextUpdateTime { get; private set; }

            public void MarkUpdated(float clock, float interval)
            {
                HasUpdated = true;
                NextUpdateTime = clock + Mathf.Max(0.0f, interval);
            }

            public void Reset()
            {
                HasUpdated = false;
                NextUpdateTime = 0.0f;
            }
        }
    }
}
