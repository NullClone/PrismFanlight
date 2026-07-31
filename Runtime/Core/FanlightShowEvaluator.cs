using System;
using System.Collections.Generic;

namespace PrismFanlight.Core
{
    internal sealed class FanlightShowEvaluator
    {
        // Fields

        private const int DefaultContributionCapacity = 32;

        private FanlightShowContribution[] _activeContributions;

        private int[] _intentFields;
        private int[] _motionFields;
        private int[] _variationFields;
        private int[] _noiseFields;
        private int[] _restFields;
        private int[] _audienceBodyFields;
        private int[] _directionFields;
        private int[] _colorFields;
        private int[] _intensityFields;


        // Methods

        internal FanlightShowEvaluator(int initialContributionCapacity = DefaultContributionCapacity)
        {
            if (initialContributionCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialContributionCapacity));
            }

            _activeContributions = initialContributionCapacity == 0
                ? Array.Empty<FanlightShowContribution>()
                : new FanlightShowContribution[initialContributionCapacity];
            _intentFields = new int[initialContributionCapacity];
            _motionFields = new int[initialContributionCapacity];
            _variationFields = new int[initialContributionCapacity];
            _noiseFields = new int[initialContributionCapacity];
            _restFields = new int[initialContributionCapacity];
            _audienceBodyFields = new int[initialContributionCapacity];
            _directionFields = new int[initialContributionCapacity];
            _colorFields = new int[initialContributionCapacity];
            _intensityFields = new int[initialContributionCapacity];
        }

        internal FanlightShowSample Evaluate(in FanlightShowEvaluationRequest request)
        {
            if (!request.Time.IsComplete)
            {
                throw new ArgumentException("A complete time sample is required.", nameof(request));
            }

            var state = FanlightShowStatePatcher.Validate(request.BaseState);
            var activeCount = 0;

            try
            {
                activeCount = CollectActive(request.Contributions.Span, request.Time.Seconds);

                Array.Sort(_activeContributions, 0, activeCount, ContributionComparer.Instance);

                PrepareOwnedFields(activeCount);
                ResolveOwnedFields(_intentFields, activeCount);
                ResolveOwnedFields(_motionFields, activeCount);
                ResolveOwnedFields(_variationFields, activeCount);
                ResolveOwnedFields(_noiseFields, activeCount);
                ResolveOwnedFields(_restFields, activeCount);
                ResolveOwnedFields(_audienceBodyFields, activeCount);
                ResolveOwnedFields(_directionFields, activeCount);
                ResolveOwnedFields(_colorFields, activeCount);
                ResolveOwnedFields(_intensityFields, activeCount);

                ValidateUniqueTrackOrders(activeCount);

                for (var i = 0; i < activeCount; i++)
                {
                    var contribution = _activeContributions[i];
                    var patch = CreateFilteredPatch(i, contribution.Patch);
                    state = FanlightShowStatePatcher.Apply(state, patch, contribution.Weight);
                }

                state = FanlightShowStatePatcher.Validate(state);
                var animationSeconds = Quantize(request.Time.Seconds, request.Options);

                return new FanlightShowSample(
                    request.Time.Seconds,
                    animationSeconds,
                    request.Time.MusicalPosition,
                    request.Time.Discontinuity,
                    state);
            }
            finally
            {
                if (activeCount > 0)
                {
                    Array.Clear(_activeContributions, 0, activeCount);
                }

                ClearOwnedFields(activeCount);
            }
        }

        private int CollectActive(ReadOnlySpan<FanlightShowContribution> contributions, double seconds)
        {
            var count = 0;

            for (var i = 0; i < contributions.Length; i++)
            {
                if (contributions[i].Weight > 0f && contributions[i].IsActive(seconds)) count++;
            }

            EnsureContributionCapacity(count);

            var destination = 0;
            for (var i = 0; i < contributions.Length; i++)
            {
                if (contributions[i].Weight > 0f && contributions[i].IsActive(seconds))
                {
                    _activeContributions[destination++] = contributions[i];
                }
            }

            return count;
        }

        private void EnsureContributionCapacity(int requiredCapacity)
        {
            if (_activeContributions.Length >= requiredCapacity) return;

            var capacity = Math.Max(DefaultContributionCapacity, _activeContributions.Length);

            while (capacity < requiredCapacity)
            {
                capacity *= 2;
            }

            Array.Resize(ref _activeContributions, capacity);
            Array.Resize(ref _intentFields, capacity);
            Array.Resize(ref _motionFields, capacity);
            Array.Resize(ref _variationFields, capacity);
            Array.Resize(ref _noiseFields, capacity);
            Array.Resize(ref _restFields, capacity);
            Array.Resize(ref _audienceBodyFields, capacity);
            Array.Resize(ref _directionFields, capacity);
            Array.Resize(ref _colorFields, capacity);
            Array.Resize(ref _intensityFields, capacity);
        }

        private void ValidateUniqueTrackOrders(int contributionCount)
        {
            for (var i = 0; i < contributionCount; i++)
            {
                var left = _activeContributions[i];

                if (left.SequenceContext == null || left.SequenceContext.IsReleased)
                {
                    throw new InvalidOperationException("A Timeline Contribution references a released Sequence Context.");
                }

                for (var j = i + 1; j < contributionCount; j++)
                {
                    var right = _activeContributions[j];

                    if (ReferenceEquals(left.SequenceContext, right.SequenceContext)
                        && left.TrackOrder == right.TrackOrder)
                    {
                        throw new InvalidOperationException($"Duplicate active Timeline Track Order in one Sequence: {left.TrackOrder}");
                    }
                }
            }
        }

        private void PrepareOwnedFields(int contributionCount)
        {
            for (var i = 0; i < contributionCount; i++)
            {
                var patch = _activeContributions[i].Patch;
                _intentFields[i] = (int)patch.Intent.Fields;
                _motionFields[i] = (int)patch.Motion.Fields;
                _variationFields[i] = (int)patch.Variation.Fields;
                _noiseFields[i] = (int)patch.Noise.Fields;
                _restFields[i] = (int)patch.Rest.Fields;
                _audienceBodyFields[i] = (int)patch.AudienceBody.Fields;
                _directionFields[i] = (int)patch.Direction.Fields;
                _colorFields[i] = (int)patch.Color.Fields;
                _intensityFields[i] = (int)patch.Intensity.Fields;
            }
        }

        private void ResolveOwnedFields(int[] fields, int contributionCount)
        {
            for (var bitIndex = 0; bitIndex < 31; bitIndex++)
            {
                var bit = 1 << bitIndex;
                FanlightSequenceContext owner = null;

                for (var i = 0; i < contributionCount; i++)
                {
                    if ((fields[i] & bit) == 0) continue;

                    var context = _activeContributions[i].SequenceContext;

                    if (context == null || context.IsReleased)
                    {
                        throw new InvalidOperationException("A Timeline Contribution references a released Sequence Context.");
                    }

                    if (owner == null || ReferenceEquals(owner, context))
                    {
                        owner = context;
                        continue;
                    }

                    if (owner.IsAncestorOf(context)) continue;

                    if (context.IsAncestorOf(owner))
                    {
                        owner = context;
                        continue;
                    }

                    throw new InvalidOperationException("Unrelated Fanlight Sequences own the same Show Field.");
                }

                if (owner == null) continue;

                var hasPartialOwner = false;
                var hasDescendant = false;

                for (var i = 0; i < contributionCount; i++)
                {
                    if ((fields[i] & bit) == 0) continue;

                    var contribution = _activeContributions[i];

                    if (ReferenceEquals(owner, contribution.SequenceContext))
                    {
                        if (contribution.Weight > 0f && contribution.Weight < 1f)
                        {
                            hasPartialOwner = true;
                        }
                    }
                    else if (owner.IsAncestorOf(contribution.SequenceContext))
                    {
                        hasDescendant = true;
                    }
                }

                if (hasPartialOwner && hasDescendant)
                {
                    throw new InvalidOperationException("A parent and child Fanlight Sequence cannot crossfade the same Show Field.");
                }

                for (var i = 0; i < contributionCount; i++)
                {
                    if (!ReferenceEquals(owner, _activeContributions[i].SequenceContext))
                    {
                        fields[i] &= ~bit;
                    }
                }
            }
        }

        private FanlightShowPatch CreateFilteredPatch(int index, in FanlightShowPatch source)
        {
            return new FanlightShowPatch(
                new FanlightIntentPatch((FanlightIntentFields)_intentFields[index], source.Intent.Value),
                new FanlightMotionPatch((FanlightMotionFields)_motionFields[index], source.Motion.Value),
                new FanlightVariationPatch((FanlightVariationFields)_variationFields[index], source.Variation.Value),
                new FanlightNoisePatch((FanlightNoiseFields)_noiseFields[index], source.Noise.Value),
                new FanlightRestPatch((FanlightRestFields)_restFields[index], source.Rest.Value),
                new FanlightAudienceBodyPatch((FanlightAudienceBodyFields)_audienceBodyFields[index], source.AudienceBody.Value),
                new FanlightDirectionPatch((FanlightDirectionFields)_directionFields[index], source.Direction.Value),
                new FanlightColorPatch((FanlightColorFields)_colorFields[index], source.Color.Value),
                new FanlightIntensityPatch((FanlightIntensityFields)_intensityFields[index], source.Intensity.Value));
        }

        private void ClearOwnedFields(int count)
        {
            if (count <= 0) return;

            Array.Clear(_intentFields, 0, count);
            Array.Clear(_motionFields, 0, count);
            Array.Clear(_variationFields, 0, count);
            Array.Clear(_noiseFields, 0, count);
            Array.Clear(_restFields, 0, count);
            Array.Clear(_audienceBodyFields, 0, count);
            Array.Clear(_directionFields, 0, count);
            Array.Clear(_colorFields, 0, count);
            Array.Clear(_intensityFields, 0, count);
        }

        private static double Quantize(double showSeconds, FanlightEvaluationOptions options)
        {
            if (options.AnimationSampleRate <= 0d) return showSeconds;

            return Math.Floor(showSeconds * options.AnimationSampleRate + options.QuantizationEpsilon) / options.AnimationSampleRate;
        }


        private sealed class ContributionComparer : IComparer<FanlightShowContribution>
        {
            internal static readonly ContributionComparer Instance = new();

            public int Compare(FanlightShowContribution left, FanlightShowContribution right)
            {
                var priority = left.TrackPriority.CompareTo(right.TrackPriority);

                if (priority != 0) return priority;

                return left.TrackOrder.CompareTo(right.TrackOrder);
            }
        }
    }
}
