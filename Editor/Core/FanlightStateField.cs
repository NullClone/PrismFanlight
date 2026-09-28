using UnityEngine;

namespace PrismFanlight.Editor
{
    internal readonly struct FanlightStateField
    {
        // Properties

        internal string PropertyName { get; }

        internal int Bit { get; }

        internal GUIContent Label { get; }

        internal bool IsPatchable => Bit != 0;


        // Methods

        internal FanlightStateField(string propertyName, int bit, string label)
        {
            PropertyName = propertyName;
            Bit = bit;
            Label = label != null ? new GUIContent(label) : null;
        }

        internal bool IsIncluded(int fields) => IsPatchable && (fields & Bit) != 0;
    }
}
