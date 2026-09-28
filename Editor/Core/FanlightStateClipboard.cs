using System;
using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor
{
    internal static class FanlightStateClipboard
    {
        // Fields

        private static readonly GUIContent CopyContent = new("Copy");
        private static readonly GUIContent PasteContent = new("Paste");

        private static FanlightStateKind? _kind;
        private static object[] _values;


        // Methods

        internal static bool CanPaste(FanlightStateKind kind) => _kind == kind && _values != null;

        internal static void AddMenuItems(
            GenericMenu menu,
            Object[] targets,
            FanlightStateKind kind,
            string propertyPath,
            Action pasted = null)
        {
            if (menu.GetItemCount() > 0)
            {
                menu.AddSeparator(string.Empty);
            }

            if (targets.Length == 1)
            {
                menu.AddItem(CopyContent, false, () => Copy(targets, kind, propertyPath));
            }
            else
            {
                menu.AddDisabledItem(CopyContent);
            }

            if (CanPaste(kind))
            {
                menu.AddItem(PasteContent, false, () =>
                {
                    if (Paste(targets, kind, propertyPath))
                    {
                        pasted?.Invoke();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(PasteContent);
            }
        }

        internal static bool Copy(Object[] targets, FanlightStateKind kind, string propertyPath)
        {
            if (targets.Length != 1 || targets[0] == null) return false;

            var source = new SerializedObject(targets[0]);
            source.Update();
            var property = source.FindProperty(propertyPath);
            if (property == null) return false;

            var fields = FanlightStateSchema.GetPatchableFields(kind);
            var values = new object[fields.Count];

            for (var i = 0; i < fields.Count; i++)
            {
                var child = property.FindPropertyRelative(fields[i].PropertyName);
                if (child == null) return false;

                values[i] = child.boxedValue;
            }

            _values = values;
            _kind = kind;
            return true;
        }

        internal static bool Paste(Object[] targets, FanlightStateKind kind, string propertyPath)
        {
            if (!CanPaste(kind) || targets.Length == 0) return false;

            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) return false;
            }

            var destination = new SerializedObject(targets);
            destination.Update();
            var property = destination.FindProperty(propertyPath);
            if (property == null) return false;

            var fields = FanlightStateSchema.GetPatchableFields(kind);

            for (var i = 0; i < fields.Count; i++)
            {
                var child = property.FindPropertyRelative(fields[i].PropertyName);
                if (child == null) return false;

                child.boxedValue = _values[i];
            }

            return destination.ApplyModifiedProperties();
        }

        internal static bool TryGetClipKind(Object[] targets, out FanlightStateKind kind)
        {
            kind = default;
            if (targets == null || targets.Length == 0 || targets[0] is not FanlightTimelineClipAsset clip) return false;

            var type = clip.GetType();
            for (var i = 1; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i].GetType() != type) return false;
            }

            kind = clip.StateKind;
            return true;
        }
    }
}
