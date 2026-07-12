using System;
using System.Collections.Generic;
using System.Linq;
using PrismFanlight.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace PrismFanlight.Editor.Timeline
{
    [CustomTimelineEditor(typeof(FanlightTimelinePlayableAsset))]
    public sealed class FanlightTimelineClipEditor : ClipEditor
    {
        private const int ColorTextureWidth = 64;
        private const float MaxColorBandHeight = 4.0f;

        private readonly Dictionary<FanlightTimelinePlayableAsset, CachedColorTexture> _colorTextures = new();


        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.highlightColor = Color.clear;
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);

            var fanlightTimelineClip = clip.asset as FanlightTimelinePlayableAsset;

            if (fanlightTimelineClip == null) return;

            if (!fanlightTimelineClip.GetTimelineOverrides().Paths
                    .Any(path => path.StartsWith("color.", StringComparison.Ordinal))) return;

            var texture = GetColorTexture(fanlightTimelineClip);
            if (texture == null) return;

            var visibleDuration = region.endTime - region.startTime;
            if (visibleDuration <= 0.0) return;

            var width = (float)(region.position.width * clip.duration / visibleDuration);
            var left = Mathf.Max((float)clip.clipIn, (float)region.startTime);
            var start = region.position.x - (float)(region.position.width * left / visibleDuration);
            var height = Mathf.Min(MaxColorBandHeight, Mathf.Max(1.0f, region.position.height * 0.42f));
            var band = new Rect(start, region.position.yMax - height, width, height);
            GUI.DrawTexture(band, texture, ScaleMode.StretchToFill, true);
        }

        private Texture2D GetColorTexture(FanlightTimelinePlayableAsset asset)
        {
            var settings = asset.GetColorSettings();
            var hash = settings.GetStableHash();

            if (_colorTextures.TryGetValue(asset, out var cached) && cached.Hash == hash && cached.Texture != null)
            {
                return cached.Texture;
            }

            if (cached.Texture != null)
            {
                Object.DestroyImmediate(cached.Texture);
            }

            var texture = new Texture2D(ColorTextureWidth, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (var i = 0; i < texture.width; i++)
            {
                var time = (float)i / (texture.width - 1);
                texture.SetPixel(i, 0, GetPreviewColor(settings, time));
            }

            texture.Apply(false, true);
            _colorTextures[asset] = new CachedColorTexture(hash, texture);
            return texture;
        }

        private static Color GetPreviewColor(FanlightColorSettings settings, float time)
        {
            switch (settings.mode)
            {
                case FanlightColorMode.Gradient:
                    return ToDisplayColor(Color.Lerp(settings.primaryColor, settings.secondaryColor, time));

                case FanlightColorMode.Random:
                    var palette = settings.paletteColors;
                    var colorCount = Mathf.Min(palette?.Length ?? 0, FanlightColorSettings.MaxPaletteColors);
                    if (colorCount > 0)
                    {
                        var index = Mathf.Min(Mathf.FloorToInt(time * colorCount), colorCount - 1);
                        return ToDisplayColor(palette[index]);
                    }

                    return ToDisplayColor(settings.primaryColor);

                default:
                    return ToDisplayColor(settings.primaryColor);
            }
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

            color.a = 0.9f;
            return color;
        }

        private readonly struct CachedColorTexture
        {
            public readonly int Hash;
            public readonly Texture2D Texture;

            public CachedColorTexture(int hash, Texture2D texture)
            {
                Hash = hash;
                Texture = texture;
            }
        }
    }
}
