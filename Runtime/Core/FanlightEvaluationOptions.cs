using System;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightEvaluationOptions
    {
        // Properties

        internal double AnimationSampleRate { get; }

        internal double QuantizationEpsilon { get; }


        // Methods

        internal FanlightEvaluationOptions(double animationSampleRate, double quantizationEpsilon)
        {
            if (double.IsNaN(animationSampleRate) || double.IsInfinity(animationSampleRate))
            {
                throw new ArgumentOutOfRangeException(nameof(animationSampleRate));
            }

            if (double.IsNaN(quantizationEpsilon) || double.IsInfinity(quantizationEpsilon) || quantizationEpsilon < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(quantizationEpsilon));
            }

            AnimationSampleRate = animationSampleRate;
            QuantizationEpsilon = quantizationEpsilon;
        }

        internal static FanlightEvaluationOptions Default => new(60d, 1e-6d);
    }
}
