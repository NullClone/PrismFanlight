using System.Collections.Generic;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor.Timeline
{
    [CustomTimelineEditor(typeof(FanlightPaletteGradientPlayableAsset))]
    public sealed class FanlightPaletteGradientClipEditor : ClipEditor
    {
        private const int TextureWidth = 128;
        private readonly Dictionary<FanlightPaletteGradientPlayableAsset, CachedTexture> _textures = new();


        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.highlightColor = Color.clear;
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);
            if (clip.asset is not FanlightPaletteGradientPlayableAsset asset) return;

            var texture = GetTexture(asset);
            if (texture == null) return;

            var visibleDuration = region.endTime - region.startTime;
            if (visibleDuration <= 0.0) return;

            var width = (float)(region.position.width * clip.duration / visibleDuration);
            var left = Mathf.Max((float)clip.clipIn, (float)region.startTime);
            var start = region.position.x - (float)(region.position.width * left / visibleDuration);
            var previewRect = new Rect(start, region.position.y, width, region.position.height);
            GUI.DrawTexture(previewRect, texture, ScaleMode.StretchToFill, true);
        }

        private Texture2D GetTexture(FanlightPaletteGradientPlayableAsset asset)
        {
            var hash = asset.GetStableHash();
            if (_textures.TryGetValue(asset, out var cached) && cached.Hash == hash && cached.Texture != null)
            {
                return cached.Texture;
            }

            if (cached.Texture != null) Object.DestroyImmediate(cached.Texture);

            var texture = new Texture2D(TextureWidth, FanlightColorSettings.PaletteSlotCount, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (var slot = 0; slot < FanlightColorSettings.PaletteSlotCount; slot++)
            {
                for (var x = 0; x < TextureWidth; x++)
                {
                    var color = asset.OverridesSlot(slot)
                        ? ToDisplayColor(asset.EvaluateSlot(slot, (float)x / (TextureWidth - 1)))
                        : new Color(0.08f, 0.08f, 0.08f, 0.35f);
                    texture.SetPixel(x, FanlightColorSettings.PaletteSlotCount - slot - 1, color);
                }
            }

            texture.Apply(false, true);
            _textures[asset] = new CachedTexture(hash, texture);
            return texture;
        }

        private static Color ToDisplayColor(Color color)
        {
            var maximum = Mathf.Max(color.r, color.g, color.b);
            if (maximum > 1.0f)
            {
                color.r /= maximum;
                color.g /= maximum;
                color.b /= maximum;
            }

            color.a = 0.92f;
            return color;
        }

        private readonly struct CachedTexture
        {
            public readonly int Hash;
            public readonly Texture2D Texture;

            public CachedTexture(int hash, Texture2D texture)
            {
                Hash = hash;
                Texture = texture;
            }
        }
    }
}
