using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrismFanlight.Timeline
{
    public sealed class FanlightTempoClip : PlayableAsset, ITimelineClipAsset
    {
        // Fields

        [SerializeField]
        private double _bpm = 120d;


        // Properties

        internal double Bpm => _bpm;

        public ClipCaps clipCaps => ClipCaps.None;


        // Methods

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Create(graph);
        }

        internal bool TryValidate(out string error)
        {
            if (!IsFinite(Bpm) || Bpm <= 0d)
            {
                error = "Tempo Section BPM must be a finite value greater than zero.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
