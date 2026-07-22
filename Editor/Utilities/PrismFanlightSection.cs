using UnityEditor.AnimatedValues;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal class PrismFanlightSection
    {
        // Fields

        internal bool expand;

        internal readonly AnimBool anim;

        private readonly GUIContent title;


        // Methods

        internal PrismFanlightSection(GUIContent title)
        {
            this.title = title;

            anim = new AnimBool(expand)
            {
                speed = 12f,
                target = expand
            };
        }

        internal void DrawHeader()
        {
            anim.target = expand = PrismFanlightEditorUtility.DrawHeader(title, expand);
        }
    }
}
