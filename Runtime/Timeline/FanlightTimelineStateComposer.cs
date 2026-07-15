using System.Collections.Generic;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTimelineStateComposer
    {
        public static FanlightResolvedState Compose(
            FanlightResolvedState baseState,
            FanlightTempoSettings baseTempo,
            IList<FanlightTimelineTrackContribution> contributions,
            float time,
            bool isTimeJump)
        {
            object color = baseState.Color;
            object motion = baseState.Motion;
            object tempo = baseTempo.Validated();
            object audience = baseState.Audience;

            foreach (var contribution in contributions)
            {
                foreach (var parameter in contribution.Parameters)
                {
                    var root = GetRoot(parameter.Descriptor.Group, color, motion, tempo, audience);
                    root = parameter.Descriptor.SetValue(root, parameter.Descriptor.Blend(parameter.Descriptor.GetValue(root), parameter.Value, parameter.Weight));
                    SetRoot(parameter.Descriptor.Group, root, ref color, ref motion, ref tempo, ref audience);
                }
            }

            var resolvedTempo = (FanlightTempoSettings)tempo;
            resolvedTempo.clockSource = FanlightTempoClockSource.ManualTime;
            resolvedTempo.manualTime = Mathf.Max(0.0f, time);

            return new FanlightResolvedState(
                resolvedTempo.Evaluate(time),
                ((FanlightMotionSettings)motion).Validated(),
                ((FanlightColorSettings)color).Validated(),
                ((FanlightAudienceSettings)audience).Validated(),
                baseState.Lod,
                baseState.Random,
                baseState.SwingTargetWorldPosition,
                baseState.LocalToWorld,
                time,
                time,
                isTimeJump);
        }


        private static object GetRoot(FanlightTimelineSettingsGroup group, object color, object motion, object tempo, object audience)
        {
            return group switch
            {
                FanlightTimelineSettingsGroup.Color => color,
                FanlightTimelineSettingsGroup.Motion => motion,
                FanlightTimelineSettingsGroup.Tempo => tempo,
                FanlightTimelineSettingsGroup.Audience => audience,
                _ => null
            };
        }

        private static void SetRoot(FanlightTimelineSettingsGroup group, object root, ref object color, ref object motion, ref object tempo, ref object audience)
        {
            switch (group)
            {
                case FanlightTimelineSettingsGroup.Color: color = root; break;
                case FanlightTimelineSettingsGroup.Motion: motion = root; break;
                case FanlightTimelineSettingsGroup.Tempo: tempo = root; break;
                case FanlightTimelineSettingsGroup.Audience: audience = root; break;
            }
        }
    }
}
