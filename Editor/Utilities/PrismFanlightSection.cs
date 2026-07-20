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

        public PrismFanlightSection(GUIContent title)
        {
            this.title = title;

            anim = new AnimBool(expand)
            {
                speed = 10f,
                target = expand
            };
        }

        public void DrawHeader()
        {
            anim.target = expand = PrismFanlightEditorUtility.DrawHeader(title, expand);
        }
    }
}
