using UnityEngine;

namespace PrismFanlight
{
    [CreateAssetMenu(menuName = "Prism Fanlight/Color Preset", fileName = "Fanlight Color Preset")]
    public sealed class FanlightColorPreset : ScriptableObject
    {
        [SerializeField]
        private FanlightColorSettings _settings = FanlightColorSettings.Default();

        public FanlightColorSettings Settings => _settings;

        public void SetSettings(FanlightColorSettings settings)
        {
            _settings = settings;
        }
    }
}
