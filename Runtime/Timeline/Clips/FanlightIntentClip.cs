using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightIntentClip : FanlightTimelineClipAsset
    {
        [SerializeField]
        private FanlightIntentState _value = FanlightTimelineDefaults.IntentState();

        internal override FanlightTimelineClipValue Value => FanlightTimelineClipValue.From(_value);
    }
}
