namespace PrismFanlight
{
    internal enum FanlightEvaluationSource
    {
        Runtime,
        Timeline
    }

    internal readonly struct FanlightEvaluationContext
    {
        public readonly FanlightEvaluationSource Source;
        public readonly float Time;
        public readonly float UpdateClock;
        public readonly bool IsTimeJump;

        private FanlightEvaluationContext(
            FanlightEvaluationSource source,
            float time,
            float updateClock,
            bool isTimeJump)
        {
            Source = source;
            Time = time;
            UpdateClock = updateClock;
            IsTimeJump = isTimeJump;
        }

        public static FanlightEvaluationContext Runtime(float time, float updateClock)
        {
            return new FanlightEvaluationContext(
                FanlightEvaluationSource.Runtime,
                time,
                updateClock,
                false);
        }

        public static FanlightEvaluationContext Timeline(float sequenceTime, bool isTimeJump)
        {
            return new FanlightEvaluationContext(
                FanlightEvaluationSource.Timeline,
                sequenceTime,
                sequenceTime,
                isTimeJump);
        }
    }
}
