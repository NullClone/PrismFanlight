using System;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    public readonly struct TimelineOverrideColorScope : IDisposable
    {
        private readonly Color _previousColor;


        public TimelineOverrideColorScope(bool overridden)
        {
            _previousColor = GUI.color;

            if (overridden)
            {
                GUI.color = Color.Lerp(_previousColor, AnimationMode.animatedPropertyColor, 0.55f);
            }
        }

        public void Dispose()
        {
            GUI.color = _previousColor;
        }
    }
}
