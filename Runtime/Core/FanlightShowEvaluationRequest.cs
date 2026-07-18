using System;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowEvaluationRequest
    {
        internal FanlightShowTimeSample Time { get; }
        internal FanlightShowState BaseState { get; }
        internal ReadOnlyMemory<FanlightShowContribution> Contributions { get; }
        internal FanlightEvaluationOptions Options { get; }

        internal FanlightShowEvaluationRequest(
            FanlightShowTimeSample time,
            FanlightShowState baseState,
            ReadOnlyMemory<FanlightShowContribution> contributions,
            FanlightEvaluationOptions options)
        {
            Time = time;
            BaseState = baseState;
            Contributions = contributions;
            Options = options;
        }
    }
}
