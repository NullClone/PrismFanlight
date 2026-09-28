using PrismFanlight.Core;
using PrismFanlight.Rendering;
using UnityEngine;

namespace PrismFanlight.Live
{
    internal sealed class FanlightStatusMonitor
    {
        // Fields

        private FanlightShowTimeFault _timeFault;
        private string _timeFaultText = string.Empty;
        private FanlightRendererFault _rendererFault;
        private string _rendererFaultText = string.Empty;
        private string _sequenceFault = string.Empty;
        private string _liveFault = string.Empty;
        private bool _isHolding;


        // Methods

        internal FanlightLiveStatus Update(
            bool isRendering,
            bool isHolding,
            bool isTimeFallbackActive,
            FanlightShowTimeFault timeFault,
            string sequenceFault,
            FanlightRendererFault rendererFault,
            string liveFault,
            Object context)
        {
            sequenceFault ??= string.Empty;
            liveFault ??= string.Empty;

            if (timeFault != _timeFault)
            {
                _timeFault = timeFault;
                _timeFaultText = timeFault == FanlightShowTimeFault.None ? string.Empty : timeFault.ToString();
                ReportFault("Time", _timeFaultText, context);
            }

            if (sequenceFault != _sequenceFault)
            {
                _sequenceFault = sequenceFault;
                ReportFault("Sequence", _sequenceFault, context);
            }

            if (rendererFault != _rendererFault)
            {
                _rendererFault = rendererFault;
                _rendererFaultText = rendererFault == FanlightRendererFault.None ? string.Empty : rendererFault.ToString();
                ReportFault("Renderer", _rendererFaultText, context);
            }

            if (liveFault != _liveFault)
            {
                _liveFault = liveFault;
                ReportFault("Live", _liveFault, context);
            }

            if (isHolding != _isHolding)
            {
                _isHolding = isHolding;
                ReportHolding(isHolding, context);
            }

            return new FanlightLiveStatus(
                isRendering,
                isHolding,
                isTimeFallbackActive,
                _timeFaultText,
                _sequenceFault,
                _rendererFaultText,
                _liveFault);
        }

        private static void ReportFault(string source, string fault, Object context)
        {
            if (!Application.isPlaying) return;

            if (string.IsNullOrEmpty(fault))
            {
                Debug.Log($"Prism Fanlight {source} Fault cleared.", context);
            }
            else
            {
                Debug.LogError($"Prism Fanlight {source} Fault: {fault}", context);
            }
        }

        private static void ReportHolding(bool isHolding, Object context)
        {
            if (!Application.isPlaying) return;

            if (isHolding)
            {
                Debug.LogWarning("Prism Fanlight is holding the last valid state.", context);
            }
            else
            {
                Debug.Log("Prism Fanlight resumed normal evaluation.", context);
            }
        }
    }
}
