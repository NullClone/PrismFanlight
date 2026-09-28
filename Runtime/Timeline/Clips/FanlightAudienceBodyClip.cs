using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightAudienceBodyClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightAudienceBodyState _value = FanlightTimelineDefaults.AudienceBodyState();

        internal override FanlightStateKind StateKind => FanlightStateKind.AudienceBody;

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
