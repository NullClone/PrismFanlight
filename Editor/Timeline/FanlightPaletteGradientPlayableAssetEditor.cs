using System;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

namespace PrismFanlight.Editor.Timeline
{
    [CustomEditor(typeof(FanlightPaletteGradientPlayableAsset))]
    public sealed class FanlightPaletteGradientPlayableAssetEditor : UnityEditor.Editor
    {
        // Fields

        private static Gradient _copiedGradient;

        private SerializedProperty _slots;
        private readonly SerializedProperty[] _gradients = new SerializedProperty[FanlightColorSettings.PaletteSlotCount];


        // Methods

        private void OnEnable()
        {
            _slots = serializedObject.FindProperty("_slots");

            for (var i = 0; i < _gradients.Length; i++)
            {
                _gradients[i] = serializedObject.FindProperty($"_slot{i + 1}");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            DrawBulkTools();
            EditorGUILayout.Space();
            DrawGradientSlots();

            if (serializedObject.ApplyModifiedProperties())
            {
                RefreshTimelinePreview();
            }
        }

        private void DrawGradientSlots()
        {
            for (var i = 0; i < _gradients.Length; i++)
            {
                var bit = 1 << i;
                var enabled = (_slots.intValue & bit) != 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    var next = EditorGUILayout.Toggle(
                        new GUIContent(string.Empty, $"Override palette Slot {i + 1}."),
                        enabled,
                        GUILayout.Width(16));
                    if (next != enabled)
                    {
                        _slots.intValue = next ? _slots.intValue | bit : _slots.intValue & ~bit;
                    }

                    using (new EditorGUI.DisabledScope(!next))
                    {
                        EditorGUILayout.PropertyField(_gradients[i], new GUIContent($"Slot {i + 1}"));
                    }

                    var slotIndex = i;
                    if (GUILayout.Button(new GUIContent("⋮", $"Editing tools for Slot {i + 1}."), GUILayout.Width(24)))
                    {
                        ShowSlotMenu(slotIndex);
                    }
                }
            }
        }

        private void ShowSlotMenu(int slotIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Gradient"), false, () => CopyGradient(slotIndex));

            if (_copiedGradient != null)
            {
                menu.AddItem(new GUIContent("Paste Gradient"), false, () => EditGradients(
                    "Paste Fanlight Gradient",
                    () => SetGradient(slotIndex, _copiedGradient)));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste Gradient"));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Apply To Enabled Slots"), false, () => EditGradients(
                "Apply Fanlight Gradient To Enabled Slots",
                () => CopyToEnabled(slotIndex)));
            menu.AddItem(new GUIContent("Apply To All Slots"), false, () => EditGradients(
                "Apply Fanlight Gradient To All Slots",
                () => CopyToAll(slotIndex)));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Make Constant/From Start"), false, () => EditGradients(
                "Make Constant Fanlight Gradient",
                () => SetConstantGradient(slotIndex, 0.0f)));
            menu.AddItem(new GUIContent("Make Constant/From End"), false, () => EditGradients(
                "Make Constant Fanlight Gradient",
                () => SetConstantGradient(slotIndex, 1.0f)));
            menu.ShowAsContext();
        }

        private void DrawBulkTools()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Override Slots", EditorStyles.boldLabel);

                if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(48)))
                {
                    _slots.intValue = (int)FanlightPaletteSlotMask.All;
                }

                if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(48)))
                {
                    _slots.intValue = (int)FanlightPaletteSlotMask.None;
                }
            }
        }

        private void CopyGradient(int slotIndex)
        {
            serializedObject.Update();
            _copiedGradient = CloneGradient(GetGradient(slotIndex));
        }

        private void CopyToEnabled(int sourceSlot)
        {
            var source = CloneGradient(GetGradient(sourceSlot));
            for (var i = 0; i < FanlightColorSettings.PaletteSlotCount; i++)
            {
                if ((_slots.intValue & (1 << i)) != 0) SetGradient(i, source);
            }
        }

        private void CopyToAll(int sourceSlot)
        {
            var source = CloneGradient(GetGradient(sourceSlot));
            for (var i = 0; i < FanlightColorSettings.PaletteSlotCount; i++)
            {
                SetGradient(i, source);
            }

            _slots.intValue = (int)FanlightPaletteSlotMask.All;
        }

        private void EditGradients(string undoName, Action edit)
        {
            serializedObject.Update();
            Undo.RecordObjects(targets, undoName);
            edit();
            serializedObject.ApplyModifiedProperties();
            RefreshTimelinePreview();
            Repaint();
        }

        private Gradient GetGradient(int slotIndex)
        {
            return _gradients[slotIndex].gradientValue;
        }

        private void SetGradient(int slotIndex, Gradient gradient)
        {
            _gradients[slotIndex].gradientValue = CloneGradient(gradient);
        }

        private void SetConstantGradient(int slotIndex, float time)
        {
            var source = GetGradient(slotIndex);
            SetGradient(slotIndex, CreateConstantGradient(source.Evaluate(time), source.colorSpace));
        }

        private static Gradient CloneGradient(Gradient source)
        {
            if (source == null) return CreateConstantGradient(Color.white, ColorSpace.Linear);

            var clone = new Gradient
            {
                mode = source.mode,
                colorSpace = source.colorSpace
            };
            clone.SetKeys(source.colorKeys, source.alphaKeys);
            return clone;
        }

        private static Gradient CreateConstantGradient(Color color, ColorSpace colorSpace)
        {
            var gradient = new Gradient { colorSpace = colorSpace };
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f) });
            return gradient;
        }

        private static void RefreshTimelinePreview()
        {
            var director = TimelineEditor.inspectedDirector;
            if (director)
            {
                director.RebuildGraph();
                director.Evaluate();
            }

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
