/*
 * Asset: OpenStamper
 * Filename: StampCompositor.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using OpenHeightmap;

namespace OpenStamper
{
    /// <summary>
    /// Composites a stamp stack onto a HeightmapData. Each enabled layer is sampled directly at the
    /// terrain's resolution and blended according to its BlendMode and Intensity, only inside the
    /// stamp's radius. The stack is processed bottom-to-top (first layer = base).
    /// </summary>
    [Serializable]
    public class StampCompositor
    {
        /// <summary>Resolution used for "Export Stack Preview" images.</summary>
        public int StampResolution = 513;

        /// <summary>
        /// Applies the full stamp stack to the given heightmap in-place.
        /// Each layer uses its own shape settings; the providers argument is kept for compatibility.
        /// </summary>
        public void Composite(HeightmapData heightmap, StampStack stack, List<IStampProvider> providers = null)
        {
            for (int i = 0; i < stack.Layers.Count; i++)
            {
                var layer = stack.Layers[i];

                if (layer == null || !layer.Enabled)
                {
                    continue;
                }

                var provider = layer.GetProvider();

                if (provider == null)
                {
                    continue;
                }

                ApplyLayer(heightmap, layer, provider);
            }

            heightmap.RecalculateBounds();
        }

        /// <summary>
        /// Previews a single stamp layer without modifying the source heightmap.
        /// Returns a new HeightmapData with the stamp applied to a copy.
        /// </summary>
        public HeightmapData PreviewSingle(HeightmapData source, StampLayer layer, IStampProvider provider = null)
        {
            var preview = source.Clone();
            ApplyLayer(preview, layer, provider ?? layer.GetProvider());
            preview.RecalculateBounds();
            return preview;
        }

        /// <summary>Previews the full stack on a copy of the source heightmap.</summary>
        public HeightmapData PreviewStack(HeightmapData source, StampStack stack, List<IStampProvider> providers = null)
        {
            var preview = source.Clone();
            Composite(preview, stack, providers);
            return preview;
        }

        /// <summary>Blends one layer into the heightmap, visiting only pixels inside its radius.</summary>
        private void ApplyLayer(HeightmapData heightmap, StampLayer layer, IStampProvider provider)
        {
            if (provider == null || layer.Radius <= 0f)
            {
                return;
            }

            int w = heightmap.Width;
            int h = heightmap.Height;

            // Only visit pixels inside the stamp's bounding square.
            int minX = Mathf.Max(0, Mathf.FloorToInt((layer.Position.x - layer.Radius) * (w - 1)));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt((layer.Position.x + layer.Radius) * (w - 1)));
            int minY = Mathf.Max(0, Mathf.FloorToInt((layer.Position.y - layer.Radius) * (h - 1)));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt((layer.Position.y + layer.Radius) * (h - 1)));

            for (int x = minX; x <= maxX; x++)
            {
                float u = (float)x / (w - 1);

                for (int y = minY; y <= maxY; y++)
                {
                    float v = (float)y / (h - 1);

                    if (!layer.Evaluate(provider, u, v, out float value, out float mask))
                    {
                        continue;
                    }

                    float current = heightmap.Heights[x, y];
                    heightmap.Heights[x, y] = Blend(current, value, layer.BlendMode, mask * layer.Intensity);
                }
            }
        }

        /// <summary>
        /// Blends a stamp value into a terrain height. weight = coverage x intensity (0 = no change).
        /// Every mode becomes lerp(terrain, modeResult, weight), so a weight of 0 always leaves the
        /// terrain untouched; that keeps Replace/Min/Average/Multiply inside the stamp.
        /// Add and Subtract allow weights above 1 (Intensity up to 2).
        /// </summary>
        public static float Blend(float terrain, float stamp, BlendMode mode, float weight)
        {
            float target = mode switch
            {
                BlendMode.Add => terrain + stamp,
                BlendMode.Subtract => terrain - stamp,
                BlendMode.Multiply => terrain * stamp,
                BlendMode.Max => Mathf.Max(terrain, stamp),
                BlendMode.Min => Mathf.Min(terrain, stamp),
                BlendMode.Average => (terrain + stamp) * 0.5f,
                BlendMode.Replace => stamp,
                BlendMode.Power => Mathf.Pow(Mathf.Max(0.0001f, terrain), Mathf.Max(0.05f, 1f - stamp)),
                _ => terrain
            };

            float w = (mode == BlendMode.Add || mode == BlendMode.Subtract)
                ? Mathf.Max(0f, weight)
                : Mathf.Clamp01(weight);

            float result = terrain + (target - terrain) * w;
            return Mathf.Clamp(result, -1f, 2f);
        }
    }
}
