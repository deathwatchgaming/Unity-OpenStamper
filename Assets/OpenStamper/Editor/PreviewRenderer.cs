/*
 * Asset: OpenStamper
 * Filename: PreviewRenderer.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System.Collections.Generic;
using UnityEngine;
using OpenHeightmap;

namespace OpenStamper
{
    /// <summary>
    /// CPU-based preview renderer for stamp composites. Maps height values
    /// to a terrain color gradient for editor display.
    /// </summary>
    public class PreviewRenderer
    {
        private Texture2D previewTexture;

        /// <summary>Reused pixel buffer (avoids allocating every render).</summary>
        private Color32[] pixelBuffer;

        /// <summary>
        /// Renders a colour preview. Callers should cache the result and only call this when the
        /// heightmap changes; pixels are written in one batch.
        /// </summary>
        public Texture2D RenderToTexture(HeightmapData heightmap)
        {
            if (heightmap == null || heightmap.Heights == null) 
            {
                return null;
            }

            int w = heightmap.Width;
            int h = heightmap.Height;

            if (previewTexture == null || previewTexture.width != w || previewTexture.height != h)
            {
                if (previewTexture != null) 
                {
                    Object.DestroyImmediate(previewTexture);
                }

                previewTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                previewTexture.hideFlags = HideFlags.HideAndDontSave;
            }

            if (pixelBuffer == null || pixelBuffer.Length != w * h)
            {
                pixelBuffer = new Color32[w * h];
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixelBuffer[y * w + x] = HeightToColor(heightmap.Heights[x, y]);
                }
            }

            previewTexture.SetPixels32(pixelBuffer);
            previewTexture.Apply();
            return previewTexture;
        }

        private Color HeightToColor(float h)
        {
            h = Mathf.Clamp01(h);

            if (h < 0.1f) 
            {
                return Color.Lerp(new Color(0f, 0f, 0.3f), new Color(0f, 0.1f, 0.5f), h / 0.1f);
            }

            if (h < 0.2f) 
            {
                return Color.Lerp(new Color(0f, 0.1f, 0.5f), new Color(0.1f, 0.4f, 0.7f), (h - 0.1f) / 0.1f);
            }

            if (h < 0.3f) 
            {
                return Color.Lerp(new Color(0.76f, 0.70f, 0.50f), new Color(0.60f, 0.65f, 0.35f), (h - 0.2f) / 0.1f);
            }

            if (h < 0.5f) 
            {
                return Color.Lerp(new Color(0.20f, 0.55f, 0.15f), new Color(0.15f, 0.40f, 0.10f), (h - 0.3f) / 0.2f);
            }

            if (h < 0.7f) 
            {
                return Color.Lerp(new Color(0.45f, 0.40f, 0.35f), new Color(0.55f, 0.50f, 0.45f), (h - 0.5f) / 0.2f);
            }

            if (h < 0.85f) 
            {
                return Color.Lerp(new Color(0.55f, 0.50f, 0.45f), new Color(0.65f, 0.62f, 0.60f), (h - 0.7f) / 0.15f);
            }
            
            return Color.Lerp(new Color(0.65f, 0.62f, 0.60f), new Color(0.95f, 0.95f, 0.97f), (h - 0.85f) / 0.15f);
        }

        public void Cleanup()
        {
            if (previewTexture != null)
            {
                Object.DestroyImmediate(previewTexture);
                previewTexture = null;
            }

            pixelBuffer = null;
        }
    }
}
