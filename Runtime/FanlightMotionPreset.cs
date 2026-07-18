using UnityEngine;

namespace PrismFanlight
{
    [CreateAssetMenu(menuName = "Prism Fanlight/Motion Preset", fileName = "Fanlight Motion Preset")]
    public sealed class FanlightMotionPreset : ScriptableObject
    {
        [SerializeField]
        private FanlightMotionSettings _settings = FanlightMotionSettings.Default();

        public FanlightMotionSettings Settings => _settings;


        public void SetSettings(FanlightMotionSettings settings)
        {
            _settings = settings;
        }
    }
}
