using System;
using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    internal static class FanlightTimelineFieldMaskResolver
    {
        internal static bool TryResolve(
            Object[] targets,
            out FanlightTimelineFieldMask mask,
            out FanlightStateKind stateKind)
        {
            mask = default;
            stateKind = default;

            if (targets.Length != 1 || TimelineEditor.inspectedAsset == null) return false;

            var target = targets[0];

            foreach (var track in TimelineEditor.inspectedAsset.GetOutputTracks())
            {
                if (track is not FanlightTimelineTrackAsset fanlightTrack) continue;

                foreach (var clip in track.GetClips())
                {
                    if (clip.asset != target) continue;

                    mask = fanlightTrack.FieldMask;
                    stateKind = fanlightTrack.StateKind;
                    return true;
                }
            }

            return false;
        }

        internal static bool IsFieldIncluded(FanlightTimelineFieldMask mask, FanlightStateKind stateKind, string propertyName)
        {
            var fieldName = ToFieldName(propertyName);

            return stateKind switch
            {
                FanlightStateKind.Intent => HasFlag(mask.Intent, fieldName),
                FanlightStateKind.Motion => HasFlag(mask.Motion, fieldName),
                FanlightStateKind.Variation => HasFlag(mask.Variation, fieldName),
                FanlightStateKind.Noise => HasFlag(mask.Noise, fieldName),
                FanlightStateKind.Rest => HasFlag(mask.Rest, fieldName),
                FanlightStateKind.AudienceBody => HasFlag(mask.AudienceBody, fieldName),
                FanlightStateKind.Direction => HasFlag(mask.Direction, fieldName),
                FanlightStateKind.Color => HasFlag(mask.Color, fieldName),
                FanlightStateKind.Intensity => HasFlag(mask.Intensity, fieldName),
                _ => true
            };
        }


        private static string ToFieldName(string propertyName)
        {
            var trimmed = propertyName.TrimStart('_');
            return trimmed.Length == 0 ? trimmed : char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        private static bool HasFlag<TFields>(TFields fields, string fieldName) where TFields : struct, Enum
        {
            return Enum.TryParse<TFields>(fieldName, out var flag) && fields.HasFlag(flag);
        }
    }
}
