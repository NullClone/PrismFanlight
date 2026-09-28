using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    internal static class FanlightTimelineFieldMaskResolver
    {
        internal static bool TryResolve(Object[] targets, out FanlightStateKind stateKind, out int fields)
        {
            stateKind = default;
            fields = 0;

            if (targets.Length != 1 || TimelineEditor.inspectedAsset == null) return false;

            var target = targets[0];

            foreach (var track in TimelineEditor.inspectedAsset.GetOutputTracks())
            {
                if (track is not FanlightTimelineTrackAsset fanlightTrack) continue;

                foreach (var clip in track.GetClips())
                {
                    if (clip.asset != target) continue;

                    stateKind = fanlightTrack.StateKind;
                    fields = GetFields(fanlightTrack.FieldMask, stateKind);
                    return true;
                }
            }

            return false;
        }


        private static int GetFields(FanlightTimelineFieldMask mask, FanlightStateKind stateKind)
        {
            return stateKind switch
            {
                FanlightStateKind.Intent => (int)mask.Intent,
                FanlightStateKind.Motion => (int)mask.Motion,
                FanlightStateKind.Variation => (int)mask.Variation,
                FanlightStateKind.Noise => (int)mask.Noise,
                FanlightStateKind.Rest => (int)mask.Rest,
                FanlightStateKind.AudienceBody => (int)mask.AudienceBody,
                FanlightStateKind.Direction => (int)mask.Direction,
                FanlightStateKind.Color => (int)mask.Color,
                FanlightStateKind.Intensity => (int)mask.Intensity,
                _ => 0
            };
        }
    }
}
