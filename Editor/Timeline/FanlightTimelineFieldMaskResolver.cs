using System;
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
            out FanlightTimelinePatchKind patchKind)
        {
            mask = default;
            patchKind = default;

            if (targets.Length != 1 || TimelineEditor.inspectedAsset == null) return false;

            var target = targets[0];

            foreach (var track in TimelineEditor.inspectedAsset.GetOutputTracks())
            {
                if (track is not FanlightTimelineTrackAsset fanlightTrack) continue;

                foreach (var clip in track.GetClips())
                {
                    if (clip.asset != target) continue;

                    mask = fanlightTrack.FieldMask;
                    patchKind = fanlightTrack.PatchKind;
                    return true;
                }
            }

            return false;
        }

        internal static bool IsFieldIncluded(FanlightTimelineFieldMask mask, FanlightTimelinePatchKind patchKind, string propertyName)
        {
            var fieldName = ToFieldName(propertyName);

            return patchKind switch
            {
                FanlightTimelinePatchKind.Intent => HasFlag(mask.Intent, fieldName),
                FanlightTimelinePatchKind.Motion => HasFlag(mask.Motion, fieldName),
                FanlightTimelinePatchKind.Variation => HasFlag(mask.Variation, fieldName),
                FanlightTimelinePatchKind.Noise => HasFlag(mask.Noise, fieldName),
                FanlightTimelinePatchKind.Rest => HasFlag(mask.Rest, fieldName),
                FanlightTimelinePatchKind.AudienceBody => HasFlag(mask.AudienceBody, fieldName),
                FanlightTimelinePatchKind.Direction => HasFlag(mask.Direction, fieldName),
                FanlightTimelinePatchKind.Color => HasFlag(mask.Color, fieldName),
                FanlightTimelinePatchKind.Intensity => HasFlag(mask.Intensity, fieldName),
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
