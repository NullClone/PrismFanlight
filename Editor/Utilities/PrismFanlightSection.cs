using UnityEditor.Rendering;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal class PrismFanlightSection
    {
        // Fields

        private readonly GUIContent title;

        private bool expand;


        // Methods

        internal PrismFanlightSection(GUIContent title)
        {
            this.title = title;
        }

        internal bool DrawHeader()
        {
            CoreEditorUtils.DrawSplitter();
            expand = CoreEditorUtils.DrawHeaderFoldout(
                title: title,
                state: expand,
                documentationURL: PrismFanlight.HelpUrl);
            return expand;
        }
    }
}
