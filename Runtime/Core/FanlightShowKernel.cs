using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    public enum FanlightContributionLayer
    {
        Base = 0,
        Scheduled = 100,
        Live = 200,
        Safety = 300
    }

    public enum FanlightReleasePolicy
    {
        RestoreUnderlying = 0,
        HoldResolvedValue = 1,
        FadeToUnderlying = 2,
        ReplaceWithCue = 3
    }

    public enum FanlightBlendProfile
    {
        Linear = 0,
        SmoothStep = 1,
        EaseIn = 2,
        EaseOut = 3,
        EaseInOut = 4
    }

    public readonly struct FanlightContribution
    {
        public string ContributionId { get; }
        public string SourceId { get; }
        public FanlightContributionLayer Layer { get; }
        public int Priority { get; }
        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public double FadeInSeconds { get; }
        public double FadeOutSeconds { get; }
        public float Weight { get; }
        public FanlightBlendProfile BlendProfile { get; }
        public FanlightReleasePolicy ReleasePolicy { get; }
        public FanlightIntentPatch Patch { get; }

        public FanlightContribution(
            string contributionId,
            string sourceId,
            FanlightContributionLayer layer,
            int priority,
            double startSeconds,
            double endSeconds,
            double fadeInSeconds,
            double fadeOutSeconds,
            float weight,
            FanlightBlendProfile blendProfile,
            FanlightReleasePolicy releasePolicy,
            FanlightIntentPatch patch)
        {
            ContributionId = contributionId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            Layer = layer;
            Priority = priority;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            FadeInSeconds = fadeInSeconds;
            FadeOutSeconds = fadeOutSeconds;
            Weight = weight;
            BlendProfile = blendProfile;
            ReleasePolicy = releasePolicy;
            Patch = patch;
        }
    }

    public interface IFanlightContributionSource
    {
        string SourceId { get; }
        FanlightContributionLayer Layer { get; }
        int Priority { get; }
        void Collect(double seconds, FanlightContributionBuffer destination);
    }

    public sealed class FanlightContributionBuffer
    {
        private FanlightContribution[] _items;

        public FanlightContributionBuffer(int capacity = 16)
        {
            _items = new FanlightContribution[Math.Max(1, capacity)];
        }

        public int Count { get; private set; }
        public int Capacity => _items.Length;

        public FanlightContribution GetAt(int index)
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }

        public void Add(in FanlightContribution contribution)
        {
            if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
            _items[Count++] = contribution;
        }

        public void Clear()
        {
            Array.Clear(_items, 0, Count);
            Count = 0;
        }
    }

    public readonly struct FanlightShowSnapshot
    {
        public string ShowId { get; }
        public int ShowVersion { get; }
        public string LayoutId { get; }
        public int LayoutVersion { get; }
        public string PersonaProfileId { get; }
        public int PersonaSchemaVersion { get; }
        public string GestureLibraryId { get; }
        public int GestureLibraryVersion { get; }
        public string CueLibraryId { get; }
        public int CueLibraryVersion { get; }
        public uint GlobalSeed { get; }
        public FanlightResolvedIntent BaseIntent { get; }

        public bool IsComplete =>
            !string.IsNullOrEmpty(ShowId)
            && ShowVersion > 0
            && !string.IsNullOrEmpty(LayoutId)
            && LayoutVersion > 0
            && !string.IsNullOrEmpty(PersonaProfileId)
            && PersonaSchemaVersion > 0
            && !string.IsNullOrEmpty(GestureLibraryId)
            && GestureLibraryVersion > 0
            && !string.IsNullOrEmpty(CueLibraryId)
            && CueLibraryVersion > 0;

        public FanlightShowSnapshot(
            string showId,
            int showVersion,
            string layoutId,
            int layoutVersion,
            string personaProfileId,
            int personaSchemaVersion,
            string gestureLibraryId,
            int gestureLibraryVersion,
            string cueLibraryId,
            int cueLibraryVersion,
            uint globalSeed,
            FanlightResolvedIntent baseIntent)
        {
            ShowId = showId ?? string.Empty;
            ShowVersion = showVersion;
            LayoutId = layoutId ?? string.Empty;
            LayoutVersion = layoutVersion;
            PersonaProfileId = personaProfileId ?? string.Empty;
            PersonaSchemaVersion = personaSchemaVersion;
            GestureLibraryId = gestureLibraryId ?? string.Empty;
            GestureLibraryVersion = gestureLibraryVersion;
            CueLibraryId = cueLibraryId ?? string.Empty;
            CueLibraryVersion = cueLibraryVersion;
            GlobalSeed = globalSeed;
            BaseIntent = baseIntent;
        }
    }

    public enum FanlightColorBlendSpace
    {
        LinearRgb = 0
    }

    public readonly struct FanlightEvaluationOptions
    {
        public double AnimationSampleRate { get; }
        public double QuantizationEpsilon { get; }
        public float DiscreteSwitchThreshold { get; }
        public FanlightColorBlendSpace ColorBlendSpace { get; }

        public FanlightEvaluationOptions(
            double animationSampleRate,
            double quantizationEpsilon,
            float discreteSwitchThreshold,
            FanlightColorBlendSpace colorBlendSpace)
        {
            AnimationSampleRate = animationSampleRate;
            QuantizationEpsilon = quantizationEpsilon;
            DiscreteSwitchThreshold = discreteSwitchThreshold;
            ColorBlendSpace = colorBlendSpace;
        }

        public static FanlightEvaluationOptions Default => new(60d, 1e-6d, 0.5f, FanlightColorBlendSpace.LinearRgb);
    }

    public readonly struct FanlightShowEvaluationRequest
    {
        public FanlightShowSnapshot Snapshot { get; }
        public FanlightShowTimeSample Time { get; }
        public FanlightContributionBuffer Contributions { get; }
        public FanlightLiveEventLog EventLog { get; }
        public int EvaluatorSchemaVersion { get; }
        public FanlightEvaluationOptions Options { get; }

        public FanlightShowEvaluationRequest(
            FanlightShowSnapshot snapshot,
            FanlightShowTimeSample time,
            FanlightContributionBuffer contributions,
            FanlightLiveEventLog eventLog,
            int evaluatorSchemaVersion,
            FanlightEvaluationOptions options)
        {
            Snapshot = snapshot;
            Time = time;
            Contributions = contributions;
            EventLog = eventLog;
            EvaluatorSchemaVersion = evaluatorSchemaVersion;
            Options = options;
        }
    }

    public readonly struct FanlightShowSample
    {
        public long SampleSequence { get; }
        public double ShowSeconds { get; }
        public double AnimationSampleSeconds { get; }
        public FanlightMusicalPosition MusicalPosition { get; }
        public FanlightResolvedIntent Intent { get; }
        public string ShowId { get; }
        public int ShowVersion { get; }
        public string LayoutId { get; }
        public int LayoutVersion { get; }
        public string PersonaProfileId { get; }
        public int PersonaSchemaVersion { get; }
        public string GestureLibraryId { get; }
        public int GestureLibraryVersion { get; }
        public string CueLibraryId { get; }
        public int CueLibraryVersion { get; }
        public string TimeDomainId { get; }
        public int TimeDomainVersion { get; }
        public string TimeProviderId { get; }
        public string TempoMapId { get; }
        public int TempoMapVersion { get; }
        public bool IsTimeFallbackActive { get; }
        public bool IsPrimaryTimeAvailable { get; }
        public uint GlobalSeed { get; }
        public int EvaluatorSchemaVersion { get; }
        public ulong SemanticHash { get; }
        public FanlightClockStatus ClockStatus { get; }
        public FanlightTimeDiscontinuity Discontinuity { get; }

        public bool IsComplete =>
            SampleSequence > 0
            && !string.IsNullOrEmpty(ShowId)
            && ShowVersion > 0
            && !string.IsNullOrEmpty(LayoutId)
            && LayoutVersion > 0
            && !string.IsNullOrEmpty(PersonaProfileId)
            && PersonaSchemaVersion > 0
            && !string.IsNullOrEmpty(GestureLibraryId)
            && GestureLibraryVersion > 0
            && !string.IsNullOrEmpty(CueLibraryId)
            && CueLibraryVersion > 0
            && !string.IsNullOrEmpty(TimeDomainId)
            && TimeDomainVersion > 0
            && !string.IsNullOrEmpty(TimeProviderId)
            && !string.IsNullOrEmpty(TempoMapId)
            && TempoMapVersion > 0
            && EvaluatorSchemaVersion > 0
            && !double.IsNaN(ShowSeconds)
            && !double.IsInfinity(ShowSeconds)
            && !double.IsNaN(AnimationSampleSeconds)
            && !double.IsInfinity(AnimationSampleSeconds)
            && MusicalPosition.IsComplete;

        public FanlightShowSample(
            long sampleSequence,
            double showSeconds,
            double animationSampleSeconds,
            FanlightMusicalPosition musicalPosition,
            FanlightResolvedIntent intent,
            string showId,
            int showVersion,
            string layoutId,
            int layoutVersion,
            string personaProfileId,
            int personaSchemaVersion,
            string gestureLibraryId,
            int gestureLibraryVersion,
            string cueLibraryId,
            int cueLibraryVersion,
            string timeDomainId,
            int timeDomainVersion,
            string timeProviderId,
            string tempoMapId,
            int tempoMapVersion,
            bool isTimeFallbackActive,
            bool isPrimaryTimeAvailable,
            uint globalSeed,
            int evaluatorSchemaVersion,
            ulong semanticHash,
            FanlightClockStatus clockStatus,
            FanlightTimeDiscontinuity discontinuity)
        {
            SampleSequence = sampleSequence;
            ShowSeconds = showSeconds;
            AnimationSampleSeconds = animationSampleSeconds;
            MusicalPosition = musicalPosition;
            Intent = intent;
            ShowId = showId ?? string.Empty;
            ShowVersion = showVersion;
            LayoutId = layoutId ?? string.Empty;
            LayoutVersion = layoutVersion;
            PersonaProfileId = personaProfileId ?? string.Empty;
            PersonaSchemaVersion = personaSchemaVersion;
            GestureLibraryId = gestureLibraryId ?? string.Empty;
            GestureLibraryVersion = gestureLibraryVersion;
            CueLibraryId = cueLibraryId ?? string.Empty;
            CueLibraryVersion = cueLibraryVersion;
            TimeDomainId = timeDomainId ?? string.Empty;
            TimeDomainVersion = timeDomainVersion;
            TimeProviderId = timeProviderId ?? string.Empty;
            TempoMapId = tempoMapId ?? string.Empty;
            TempoMapVersion = tempoMapVersion;
            IsTimeFallbackActive = isTimeFallbackActive;
            IsPrimaryTimeAvailable = isPrimaryTimeAvailable;
            GlobalSeed = globalSeed;
            EvaluatorSchemaVersion = evaluatorSchemaVersion;
            SemanticHash = semanticHash;
            ClockStatus = clockStatus;
            Discontinuity = discontinuity;
        }
    }

    public interface IFanlightShowEvaluator
    {
        int SchemaVersion { get; }
        FanlightShowSample Evaluate(in FanlightShowEvaluationRequest request);
    }

    public sealed class FanlightShowEvaluator : IFanlightShowEvaluator
    {
        private int[] _order = Array.Empty<int>();
        private FanlightExpertParameterValue[] _expert = Array.Empty<FanlightExpertParameterValue>();
        private int _expertCount;

        public FanlightShowEvaluator(int schemaVersion = 1)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            SchemaVersion = schemaVersion;
        }

        public int SchemaVersion { get; }

        public FanlightShowSample Evaluate(in FanlightShowEvaluationRequest request)
        {
            if (!request.Time.IsComplete) throw new ArgumentException("A complete show time sample is required.", nameof(request));
            if (!request.Snapshot.IsComplete) throw new ArgumentException("A complete show snapshot is required.", nameof(request));
            if (request.EvaluatorSchemaVersion != SchemaVersion) throw new InvalidOperationException("Evaluator schema version mismatch.");
            var contributions = request.Contributions ?? throw new ArgumentNullException(nameof(request.Contributions));
            EnsureOrder(contributions.Count);
            Sort(contributions);

            var options = Normalize(request.Options);
            var resolved = request.Snapshot.BaseIntent;
            CopyExpert(resolved.Expert);

            for (var i = 0; i < contributions.Count; i++)
            {
                var contribution = contributions.GetAt(_order[i]);
                Validate(contribution);
                if (i > 0 && Compare(contributions.GetAt(_order[i - 1]), contribution) == 0)
                {
                    throw new InvalidOperationException($"Duplicate contribution key: {contribution.ContributionId}");
                }

                var weight = EvaluateWeight(contribution, request.Time.Seconds);
                if (weight <= 0f) continue;
                resolved = Apply(resolved, contribution.Patch, weight, options.DiscreteSwitchThreshold);
            }

            resolved = NormalizeResolvedIntent(WithExpert(resolved));
            var sampleSeconds = options.AnimationSampleRate <= 0d
                ? request.Time.Seconds
                : Math.Floor(request.Time.Seconds * options.AnimationSampleRate + options.QuantizationEpsilon)
                  / options.AnimationSampleRate;

            var semanticHash = ComputeSemanticHash(request, resolved, contributions, options);
            return new FanlightShowSample(
                request.Time.Sequence,
                request.Time.Seconds,
                sampleSeconds,
                request.Time.MusicalPosition,
                resolved,
                request.Snapshot.ShowId,
                request.Snapshot.ShowVersion,
                request.Snapshot.LayoutId,
                request.Snapshot.LayoutVersion,
                request.Snapshot.PersonaProfileId,
                request.Snapshot.PersonaSchemaVersion,
                request.Snapshot.GestureLibraryId,
                request.Snapshot.GestureLibraryVersion,
                request.Snapshot.CueLibraryId,
                request.Snapshot.CueLibraryVersion,
                request.Time.TimeDomainId,
                request.Time.TimeDomainVersion,
                request.Time.ProviderId,
                request.Time.TempoMapId,
                request.Time.TempoMapVersion,
                request.Time.IsFallbackActive,
                request.Time.IsPrimaryAvailable,
                request.Snapshot.GlobalSeed,
                SchemaVersion,
                semanticHash,
                request.Time.Status,
                request.Time.Discontinuity);
        }

        private static FanlightEvaluationOptions Normalize(FanlightEvaluationOptions options)
        {
            var epsilon = options.QuantizationEpsilon > 0d && IsFinite(options.QuantizationEpsilon)
                ? options.QuantizationEpsilon
                : 1e-6d;
            var threshold = Clamp01(options.DiscreteSwitchThreshold);
            var rate = IsFinite(options.AnimationSampleRate) ? options.AnimationSampleRate : 0d;
            return new FanlightEvaluationOptions(rate, epsilon, threshold, FanlightColorBlendSpace.LinearRgb);
        }

        private void EnsureOrder(int count)
        {
            if (_order.Length < count) Array.Resize(ref _order, Math.Max(count, _order.Length == 0 ? 16 : _order.Length * 2));
            for (var i = 0; i < count; i++) _order[i] = i;
        }

        private void Sort(FanlightContributionBuffer buffer)
        {
            for (var i = 1; i < buffer.Count; i++)
            {
                var key = _order[i];
                var j = i - 1;
                while (j >= 0 && Compare(buffer.GetAt(_order[j]), buffer.GetAt(key)) > 0)
                {
                    _order[j + 1] = _order[j];
                    j--;
                }

                _order[j + 1] = key;
            }
        }

        private static int Compare(FanlightContribution left, FanlightContribution right)
        {
            var layer = ((int)left.Layer).CompareTo((int)right.Layer);
            if (layer != 0) return layer;
            var priority = left.Priority.CompareTo(right.Priority);
            if (priority != 0) return priority;
            var source = string.Compare(left.SourceId, right.SourceId, StringComparison.Ordinal);
            return source != 0 ? source : string.Compare(left.ContributionId, right.ContributionId, StringComparison.Ordinal);
        }

        private static void Validate(FanlightContribution contribution)
        {
            if (string.IsNullOrEmpty(contribution.SourceId) || string.IsNullOrEmpty(contribution.ContributionId))
                throw new InvalidOperationException("Contribution and source IDs are required.");
            if (!IsFinite(contribution.StartSeconds) || (double.IsNaN(contribution.EndSeconds)))
                throw new InvalidOperationException("Contribution time is invalid.");
            if (contribution.EndSeconds < contribution.StartSeconds)
                throw new InvalidOperationException("Contribution end must not precede start.");
            if (contribution.FadeInSeconds < 0d || contribution.FadeOutSeconds < 0d)
                throw new InvalidOperationException("Contribution fades must be non-negative.");
        }

        private static float EvaluateWeight(FanlightContribution contribution, double seconds)
        {
            if (seconds < contribution.StartSeconds) return 0f;
            if (seconds > contribution.EndSeconds && contribution.ReleasePolicy != FanlightReleasePolicy.HoldResolvedValue) return 0f;

            var weight = Clamp01(contribution.Weight);
            if (contribution.FadeInSeconds > 0d && seconds < contribution.StartSeconds + contribution.FadeInSeconds)
            {
                weight *= Shape((float)((seconds - contribution.StartSeconds) / contribution.FadeInSeconds), contribution.BlendProfile);
            }

            if (contribution.ReleasePolicy != FanlightReleasePolicy.HoldResolvedValue
                && contribution.FadeOutSeconds > 0d
                && seconds > contribution.EndSeconds - contribution.FadeOutSeconds)
            {
                weight *= Shape((float)((contribution.EndSeconds - seconds) / contribution.FadeOutSeconds), contribution.BlendProfile);
            }

            return Clamp01(weight);
        }

        private static float Shape(float value, FanlightBlendProfile profile)
        {
            var t = Clamp01(value);
            return profile switch
            {
                FanlightBlendProfile.SmoothStep => t * t * (3f - 2f * t),
                FanlightBlendProfile.EaseIn => t * t,
                FanlightBlendProfile.EaseOut => 1f - (1f - t) * (1f - t),
                FanlightBlendProfile.EaseInOut => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t),
                _ => t
            };
        }

        private FanlightResolvedIntent Apply(FanlightResolvedIntent current, FanlightIntentPatch patch, float weight, float threshold)
        {
            var palette = patch.HasPalette ? ApplyPalette(current.Palette, patch.Palette, weight) : current.Palette;
            if (patch.HasExpert) ApplyExpert(patch.Expert, weight);
            return new FanlightResolvedIntent(
                patch.HasGestureId && weight >= threshold ? patch.GestureId : current.GestureId,
                patch.HasHandZone ? Lerp(current.HandZone, patch.HandZone, weight) : current.HandZone,
                patch.HasEnergy ? Lerp(current.Energy, patch.Energy, weight) : current.Energy,
                patch.HasParticipation ? Lerp(current.Participation, patch.Participation, weight) : current.Participation,
                patch.HasSynchronization ? Lerp(current.Synchronization, patch.Synchronization, weight) : current.Synchronization,
                patch.HasRealism ? Lerp(current.Realism, patch.Realism, weight) : current.Realism,
                patch.HasReach ? Lerp(current.Reach, patch.Reach, weight) : current.Reach,
                patch.HasDirection ? Lerp(current.Direction, patch.Direction, weight, threshold) : current.Direction,
                palette,
                patch.HasPenlightsEnabled && weight >= threshold ? patch.PenlightsEnabled : current.PenlightsEnabled,
                patch.HasAudienceBodiesEnabled && weight >= threshold ? patch.AudienceBodiesEnabled : current.AudienceBodiesEnabled,
                current.Expert);
        }

        private static FanlightPaletteIntent ApplyPalette(FanlightPaletteIntent current, FanlightPalettePatch patch, float weight)
        {
            var mask = patch.Fields;
            var target = patch.Value;
            return new FanlightPaletteIntent(
                (mask & FanlightPaletteFieldMask.Slot1) != 0 ? Color.LerpUnclamped(current.Slot1, target.Slot1, weight) : current.Slot1,
                (mask & FanlightPaletteFieldMask.Slot2) != 0 ? Color.LerpUnclamped(current.Slot2, target.Slot2, weight) : current.Slot2,
                (mask & FanlightPaletteFieldMask.Slot3) != 0 ? Color.LerpUnclamped(current.Slot3, target.Slot3, weight) : current.Slot3,
                (mask & FanlightPaletteFieldMask.Slot4) != 0 ? Color.LerpUnclamped(current.Slot4, target.Slot4, weight) : current.Slot4,
                (mask & FanlightPaletteFieldMask.Slot5) != 0 ? Color.LerpUnclamped(current.Slot5, target.Slot5, weight) : current.Slot5,
                (mask & FanlightPaletteFieldMask.Slot6) != 0 ? Color.LerpUnclamped(current.Slot6, target.Slot6, weight) : current.Slot6,
                (mask & FanlightPaletteFieldMask.GlobalIntensity) != 0 ? Lerp(current.GlobalIntensity, target.GlobalIntensity, weight) : current.GlobalIntensity,
                (mask & FanlightPaletteFieldMask.RandomIntensity) != 0 ? Lerp(current.RandomIntensity, target.RandomIntensity, weight) : current.RandomIntensity);
        }

        private void CopyExpert(FanlightExpertPatch patch)
        {
            var ids = FanlightExpertSchema.ParameterIds;
            EnsureExpert(ids.Length);
            _expertCount = ids.Length;
            for (var i = 0; i < ids.Length; i++) _expert[i] = FanlightExpertSchema.Get(ids[i]).DefaultParameterValue;

            var span = patch.Values.Span;
            ValidateExpertPatchOrder(span);
            for (var i = 0; i < span.Length; i++)
            {
                var normalized = FanlightExpertSchema.NormalizeResolved(span[i]);
                var index = FindExpert(normalized.ParameterId);
                if (index < 0) throw new InvalidOperationException($"Expert schema does not contain {normalized.ParameterId}.");
                _expert[index] = normalized;
            }
        }

        private void ApplyExpert(FanlightExpertPatch patch, float contributionWeight)
        {
            var values = patch.Values.Span;
            ValidateExpertPatchOrder(values);
            for (var i = 0; i < values.Length; i++)
            {
                var incoming = values[i];
                FanlightExpertSchema.ValidateInput(incoming);
                var definition = FanlightExpertSchema.Get(incoming.ParameterId);
                var index = FindExpert(incoming.ParameterId);
                var effectiveWeight = Clamp01(contributionWeight * incoming.Weight);
                if (index < 0)
                {
                    EnsureExpert(_expertCount + 1);
                    index = ~index;
                    for (var move = _expertCount; move > index; move--) _expert[move] = _expert[move - 1];
                    _expert[index] = definition.DefaultParameterValue;
                    _expertCount++;
                }

                var current = _expert[index];
                if (incoming.ValueKind == FanlightExpertValueKind.Integer)
                {
                    var value = BlendInteger(current.IntegerValue, incoming.IntegerValue, incoming.BlendMode, effectiveWeight);
                    _expert[index] = FanlightExpertParameterValue.Integer(incoming.ParameterId, definition.Clamp(value));
                }
                else
                {
                    var value = BlendFloat(current.FloatValue, incoming.FloatValue, incoming.BlendMode, effectiveWeight);
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        throw new InvalidOperationException($"Expert parameter {incoming.ParameterId} resolved to a non-finite value.");
                    _expert[index] = FanlightExpertParameterValue.Float(incoming.ParameterId, definition.Clamp(value));
                }
            }
        }

        private static void ValidateExpertPatchOrder(ReadOnlySpan<FanlightExpertParameterValue> values)
        {
            for (var i = 1; i < values.Length; i++)
            {
                if ((int)values[i - 1].ParameterId >= (int)values[i].ParameterId)
                    throw new InvalidOperationException("Expert patch IDs must be unique and strictly ascending.");
            }
        }

        private int FindExpert(FanlightExpertParameterId id)
        {
            var low = 0;
            var high = _expertCount - 1;
            while (low <= high)
            {
                var middle = (low + high) >> 1;
                var comparison = ((int)_expert[middle].ParameterId).CompareTo((int)id);
                if (comparison == 0) return middle;
                if (comparison < 0) low = middle + 1;
                else high = middle - 1;
            }

            return ~low;
        }

        private void EnsureExpert(int count)
        {
            if (_expert.Length < count) Array.Resize(ref _expert, Math.Max(count, _expert.Length == 0 ? 16 : _expert.Length * 2));
        }

        private FanlightResolvedIntent WithExpert(FanlightResolvedIntent intent)
        {
            if (_expertCount == 0)
            {
                return new FanlightResolvedIntent(intent.GestureId, intent.HandZone, intent.Energy, intent.Participation,
                    intent.Synchronization, intent.Realism, intent.Reach, intent.Direction, intent.Palette,
                    intent.PenlightsEnabled, intent.AudienceBodiesEnabled, FanlightExpertPatch.Empty);
            }

            var copy = new FanlightExpertParameterValue[_expertCount];
            Array.Copy(_expert, copy, _expertCount);
            return new FanlightResolvedIntent(intent.GestureId, intent.HandZone, intent.Energy, intent.Participation,
                intent.Synchronization, intent.Realism, intent.Reach, intent.Direction, intent.Palette,
                intent.PenlightsEnabled, intent.AudienceBodiesEnabled, new FanlightExpertPatch(copy));
        }

        private static FanlightResolvedIntent NormalizeResolvedIntent(FanlightResolvedIntent intent)
        {
            if (string.IsNullOrEmpty(intent.GestureId)) throw new InvalidOperationException("Resolved gesture ID is required.");
            ValidateFinite(intent.HandZone.HeightOffset, nameof(intent.HandZone.HeightOffset));
            ValidateFinite(intent.HandZone.ForwardOffset, nameof(intent.HandZone.ForwardOffset));
            ValidateFinite(intent.HandZone.SideOffset, nameof(intent.HandZone.SideOffset));
            ValidateFinite(intent.Energy, nameof(intent.Energy));
            ValidateFinite(intent.Participation, nameof(intent.Participation));
            ValidateFinite(intent.Synchronization, nameof(intent.Synchronization));
            ValidateFinite(intent.Realism, nameof(intent.Realism));
            ValidateFinite(intent.Reach, nameof(intent.Reach));
            ValidateFinite(intent.Direction.WorldYawDegrees, nameof(intent.Direction.WorldYawDegrees));
            ValidateFinite(intent.Direction.WorldPitchDegrees, nameof(intent.Direction.WorldPitchDegrees));
            ValidateFinite(intent.Direction.TargetWorldPosition.x, nameof(intent.Direction.TargetWorldPosition));
            ValidateFinite(intent.Direction.TargetWorldPosition.y, nameof(intent.Direction.TargetWorldPosition));
            ValidateFinite(intent.Direction.TargetWorldPosition.z, nameof(intent.Direction.TargetWorldPosition));
            ValidateFinite(intent.Direction.AimStrength, nameof(intent.Direction.AimStrength));
            ValidateFinite(intent.Direction.FallbackWorldYawDegrees, nameof(intent.Direction.FallbackWorldYawDegrees));
            ValidateFinite(intent.Direction.FallbackWorldPitchDegrees, nameof(intent.Direction.FallbackWorldPitchDegrees));
            ValidatePalette(intent.Palette);

            var palette = new FanlightPaletteIntent(
                intent.Palette.Slot1, intent.Palette.Slot2, intent.Palette.Slot3,
                intent.Palette.Slot4, intent.Palette.Slot5, intent.Palette.Slot6,
                Math.Max(0f, intent.Palette.GlobalIntensity), Clamp01(intent.Palette.RandomIntensity));
            var direction = new FanlightDirectionIntent(
                intent.Direction.Mode,
                intent.Direction.WorldYawDegrees,
                intent.Direction.WorldPitchDegrees,
                intent.Direction.TargetWorldPosition,
                intent.Direction.TargetBindingId,
                Clamp01(intent.Direction.AimStrength),
                intent.Direction.FallbackWorldYawDegrees,
                intent.Direction.FallbackWorldPitchDegrees);
            return new FanlightResolvedIntent(
                intent.GestureId, intent.HandZone,
                Clamp01(intent.Energy), Clamp01(intent.Participation), Clamp01(intent.Synchronization),
                Clamp01(intent.Realism), Clamp01(intent.Reach), direction, palette,
                intent.PenlightsEnabled, intent.AudienceBodiesEnabled, intent.Expert);
        }

        private static void ValidatePalette(FanlightPaletteIntent palette)
        {
            ValidateColor(palette.Slot1);
            ValidateColor(palette.Slot2);
            ValidateColor(palette.Slot3);
            ValidateColor(palette.Slot4);
            ValidateColor(palette.Slot5);
            ValidateColor(palette.Slot6);
            ValidateFinite(palette.GlobalIntensity, nameof(palette.GlobalIntensity));
            ValidateFinite(palette.RandomIntensity, nameof(palette.RandomIntensity));
        }

        private static void ValidateColor(Color value)
        {
            ValidateFinite(value.r, "Color.r");
            ValidateFinite(value.g, "Color.g");
            ValidateFinite(value.b, "Color.b");
            ValidateFinite(value.a, "Color.a");
        }

        private static void ValidateFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidOperationException($"Resolved value {name} must be finite.");
        }

        private ulong ComputeSemanticHash(
            in FanlightShowEvaluationRequest request,
            FanlightResolvedIntent intent,
            FanlightContributionBuffer contributions,
            FanlightEvaluationOptions options)
        {
            var hash = new StableHash64();
            hash.Add(SchemaVersion);
            hash.Add(FanlightExpertSchema.Version);
            hash.Add(request.Snapshot.ShowId);
            hash.Add(request.Snapshot.ShowVersion);
            hash.Add(request.Snapshot.LayoutId);
            hash.Add(request.Snapshot.LayoutVersion);
            hash.Add(request.Snapshot.PersonaProfileId);
            hash.Add(request.Snapshot.PersonaSchemaVersion);
            hash.Add(request.Snapshot.GestureLibraryId);
            hash.Add(request.Snapshot.GestureLibraryVersion);
            hash.Add(request.Snapshot.CueLibraryId);
            hash.Add(request.Snapshot.CueLibraryVersion);
            hash.Add(request.Time.TimeDomainId);
            hash.Add(request.Time.TimeDomainVersion);
            hash.Add(request.Time.TempoMapId);
            hash.Add(request.Time.TempoMapVersion);
            hash.Add(request.Snapshot.GlobalSeed);
            hash.AddQuantized(request.Time.Seconds, options.QuantizationEpsilon);
            AddMusical(ref hash, request.Time.MusicalPosition, options.QuantizationEpsilon);
            AddIntent(ref hash, intent, options.QuantizationEpsilon);
            for (var i = 0; i < contributions.Count; i++)
            {
                var contribution = contributions.GetAt(_order[i]);
                var weight = EvaluateWeight(contribution, request.Time.Seconds);
                if (weight <= 0f) continue;
                hash.Add(contribution.ContributionId);
                hash.Add(contribution.SourceId);
                hash.AddQuantized(weight, options.QuantizationEpsilon);
            }

            return hash.Value;
        }

        private static void AddMusical(ref StableHash64 hash, FanlightMusicalPosition value, double epsilon)
        {
            hash.AddQuantized(value.Seconds, epsilon);
            hash.AddQuantized(value.Beat, epsilon);
            hash.Add(value.Bar);
            hash.AddQuantized(value.BeatInBar, epsilon);
            hash.AddQuantized(value.BeatPhase, epsilon);
            hash.AddQuantized(value.BarPhase, epsilon);
            hash.AddQuantized(value.Bpm, epsilon);
            hash.Add(value.BeatsPerBar);
            hash.Add(value.BeatUnit);
            hash.Add(value.TempoSegmentId);
        }

        private static void AddIntent(ref StableHash64 hash, FanlightResolvedIntent value, double epsilon)
        {
            hash.Add(value.GestureId);
            hash.Add((int)value.HandZone.Zone);
            hash.AddQuantized(value.HandZone.HeightOffset, epsilon);
            hash.AddQuantized(value.HandZone.ForwardOffset, epsilon);
            hash.AddQuantized(value.HandZone.SideOffset, epsilon);
            hash.AddQuantized(value.Energy, epsilon);
            hash.AddQuantized(value.Participation, epsilon);
            hash.AddQuantized(value.Synchronization, epsilon);
            hash.AddQuantized(value.Realism, epsilon);
            hash.AddQuantized(value.Reach, epsilon);
            hash.Add((int)value.Direction.Mode);
            hash.AddQuantized(value.Direction.WorldYawDegrees, epsilon);
            hash.AddQuantized(value.Direction.WorldPitchDegrees, epsilon);
            hash.AddQuantized(value.Direction.TargetWorldPosition.x, epsilon);
            hash.AddQuantized(value.Direction.TargetWorldPosition.y, epsilon);
            hash.AddQuantized(value.Direction.TargetWorldPosition.z, epsilon);
            hash.Add(value.Direction.TargetBindingId);
            hash.AddQuantized(value.Direction.AimStrength, epsilon);
            AddColor(ref hash, value.Palette.Slot1, epsilon);
            AddColor(ref hash, value.Palette.Slot2, epsilon);
            AddColor(ref hash, value.Palette.Slot3, epsilon);
            AddColor(ref hash, value.Palette.Slot4, epsilon);
            AddColor(ref hash, value.Palette.Slot5, epsilon);
            AddColor(ref hash, value.Palette.Slot6, epsilon);
            hash.AddQuantized(value.Palette.GlobalIntensity, epsilon);
            hash.AddQuantized(value.Palette.RandomIntensity, epsilon);
            hash.Add(value.PenlightsEnabled);
            hash.Add(value.AudienceBodiesEnabled);
            var expert = value.Expert.Values.Span;
            for (var i = 0; i < expert.Length; i++)
            {
                hash.Add((int)expert[i].ParameterId);
                hash.Add((int)expert[i].ValueKind);
                if (expert[i].ValueKind == FanlightExpertValueKind.Integer) hash.Add(expert[i].IntegerValue);
                else hash.AddQuantized(expert[i].FloatValue, epsilon);
            }
        }

        private static void AddColor(ref StableHash64 hash, Color value, double epsilon)
        {
            hash.AddQuantized(value.r, epsilon);
            hash.AddQuantized(value.g, epsilon);
            hash.AddQuantized(value.b, epsilon);
            hash.AddQuantized(value.a, epsilon);
        }

        private static FanlightHandZoneIntent Lerp(FanlightHandZoneIntent left, FanlightHandZoneIntent right, float weight) =>
            new(weight >= 0.5f ? right.Zone : left.Zone,
                Lerp(left.HeightOffset, right.HeightOffset, weight),
                Lerp(left.ForwardOffset, right.ForwardOffset, weight),
                Lerp(left.SideOffset, right.SideOffset, weight));

        private static FanlightDirectionIntent Lerp(FanlightDirectionIntent left, FanlightDirectionIntent right, float weight, float threshold) =>
            new(weight >= threshold ? right.Mode : left.Mode,
                LerpAngle(left.WorldYawDegrees, right.WorldYawDegrees, weight),
                Lerp(left.WorldPitchDegrees, right.WorldPitchDegrees, weight),
                Vector3.LerpUnclamped(left.TargetWorldPosition, right.TargetWorldPosition, weight),
                weight >= threshold ? right.TargetBindingId : left.TargetBindingId,
                Lerp(left.AimStrength, right.AimStrength, weight),
                LerpAngle(left.FallbackWorldYawDegrees, right.FallbackWorldYawDegrees, weight),
                Lerp(left.FallbackWorldPitchDegrees, right.FallbackWorldPitchDegrees, weight));

        private static float BlendFloat(float current, float incoming, FanlightExpertBlendMode mode, float weight) => mode switch
        {
            FanlightExpertBlendMode.Add => current + incoming * weight,
            FanlightExpertBlendMode.Multiply => current * Lerp(1f, incoming, weight),
            _ => Lerp(current, incoming, weight)
        };

        private static int BlendInteger(int current, int incoming, FanlightExpertBlendMode mode, float weight) =>
            (int)Math.Round(BlendFloat(current, incoming, mode, weight), MidpointRounding.AwayFromZero);

        private static float Lerp(float left, float right, float weight) => left + (right - left) * weight;

        private static float LerpAngle(float left, float right, float weight)
        {
            var delta = ((right - left + 540f) % 360f) - 180f;
            return left + delta * weight;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private struct StableHash64
        {
            private const ulong Offset = 14695981039346656037UL;
            private const ulong Prime = 1099511628211UL;
            private ulong _value;
            public ulong Value => _value == 0UL ? Offset : _value;

            private void Byte(byte value)
            {
                if (_value == 0UL) _value = Offset;
                _value ^= value;
                _value *= Prime;
            }

            public void Add(bool value) => Byte(value ? (byte)1 : (byte)0);
            public void Add(int value) => Add((long)value);
            public void Add(uint value) => Add((ulong)value);
            public void Add(long value) => Add(unchecked((ulong)value));

            public void Add(ulong value)
            {
                for (var i = 0; i < 8; i++) Byte((byte)(value >> (i * 8)));
            }

            public void Add(string value)
            {
                value ??= string.Empty;
                Add(value.Length);
                for (var i = 0; i < value.Length; i++)
                {
                    Byte((byte)value[i]);
                    Byte((byte)(value[i] >> 8));
                }
            }

            public void AddQuantized(double value, double epsilon)
            {
                if (!IsFinite(value))
                {
                    Add(BitConverter.DoubleToInt64Bits(value));
                    return;
                }

                var scaled = value / epsilon;
                if (scaled >= long.MaxValue || scaled <= long.MinValue) Add(BitConverter.DoubleToInt64Bits(value));
                else Add((long)Math.Round(scaled, MidpointRounding.AwayFromZero));
            }
        }
    }
}
