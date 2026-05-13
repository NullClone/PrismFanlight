using UnityEngine;

namespace PrismFanlight
{
    [CreateAssetMenu(menuName = "Prism Fanlight/Audience Layout Preset", fileName = "Audience Layout Preset")]
    public sealed class AudienceLayoutPreset : ScriptableObject
    {
        [SerializeField]
        private Audience _audience = Audience.Default();

        public Audience Audience => _audience;

        public void SetAudience(Audience audience)
        {
            _audience = audience;
        }
    }
}
