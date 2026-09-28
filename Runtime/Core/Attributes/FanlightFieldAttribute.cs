using System;

namespace PrismFanlight.Core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class FanlightFieldAttribute : Attribute
    {
        // Properties

        internal object Field { get; }

        internal string Label { get; }


        // Methods

        internal FanlightFieldAttribute() { }

        internal FanlightFieldAttribute(object field, string label = null)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Label = label;
        }
    }
}
