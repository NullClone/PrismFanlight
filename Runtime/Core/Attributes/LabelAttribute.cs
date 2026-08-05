using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; }


        public LabelAttribute(string label)
        {
            Label = label;
        }
    }
}
