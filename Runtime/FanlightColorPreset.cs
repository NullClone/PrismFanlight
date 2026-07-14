using UnityEngine;

namespace PrismFanlight
{
    [CreateAssetMenu(menuName = "Prism Fanlight/Color Preset", fileName = "Fanlight Color Preset")]
    public sealed class FanlightColorPreset : ScriptableObject
    {
        [SerializeField]
        private FanlightColorSettings _settings = FanlightColorSettings.Default();

        public FanlightColorSettings Settings => _settings.Validated();

        private void OnValidate()
        {
            _settings = _settings.Validated();
        }

        public void SetSettings(FanlightColorSettings settings)
        {
            _settings = settings.Validated();
        }
    }
}
