/*
 * Asset: OpenStamper
 * Filename: StampExporter.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using OpenHeightmap;

namespace OpenStamper
{
    public static class StampExporter
    {
        /// <summary>
        /// Exports the current heightmap (after stamp composition) to a file.
        /// Supports PNG (8-bit), RAW16 (16-bit), and EXR (32-bit float).
        /// Delegates to OpenHeightmap.HeightmapExporter for actual file writing.
        /// </summary>
        public static bool ExportHeightmap(HeightmapData heightmap, string filePath, OpenHeightmap.ExportFormat format)
        {
            if (heightmap == null)
            {
                Debug.LogError("[OpenStamper] No heightmap to export.");
                return false;
            }

            return OpenHeightmap.HeightmapExporter.Export(heightmap, filePath, format);
        }

        /// <summary>
        /// Exports a single stamp shape as a grayscale PNG.
        /// Useful for creating stamp assets or importing into other tools.
        /// </summary>
        public static bool ExportStampAsPNG(IStampProvider provider, string filePath, int resolution = 257, bool confirmOverwrite = false)
        {
            if (confirmOverwrite && !ConfirmOverwrite(filePath)) 
            {
                return false;
            }

            try
            {
                Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

                for (int x = 0; x < resolution; x++)
                {
                    for (int y = 0; y < resolution; y++)
                    {
                        float u = (float)x / (resolution - 1);
                        float v = (float)y / (resolution - 1);
                        float val = Mathf.Clamp01(provider.Sample(u, v));
                        tex.SetPixel(x, y, new Color(val, val, val, 1f));
                    }
                }

                tex.Apply();

                string directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(filePath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                AssetDatabase.Refresh();
                Debug.Log($"[OpenStamper] Stamp exported: {filePath} ({resolution}x{resolution})");
                return true;
            }

            catch (Exception ex)
            {
                Debug.LogError($"[OpenStamper] Export failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exports the stamp stack visualization as a composite preview image.
        /// Shows all stamps blended together.
        /// </summary>
        public static bool ExportStackPreview(StampStack stack, System.Collections.Generic.List<IStampProvider> providers, string filePath, int resolution = 513, bool confirmOverwrite = false)
        {
            if (confirmOverwrite && !ConfirmOverwrite(filePath)) 
            {
                return false;
            }

            try
            {
                // Composite the stack onto a flat map with the same blending used for terrain.
                var blank = new HeightmapData(resolution);
                var compositor = new StampCompositor();
                compositor.Composite(blank, stack, providers);
                blank.Normalize();

                Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[resolution * resolution];

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        byte val = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(blank.Heights[x, y]) * 255f), 0, 255);
                        pixels[y * resolution + x] = new Color32(val, val, val, 255);
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                string directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(filePath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                AssetDatabase.Refresh();
                Debug.Log($"[OpenStamper] Stack preview exported: {filePath}");
                return true;
            }

            catch (Exception ex)
            {
                Debug.LogError($"[OpenStamper] Export failed: {ex.Message}");
                return false;
            }
        }

        public static string GetExportPath(string defaultName, string extension)
        {
            return EditorUtility.SaveFilePanel(
                "Export Stamp",
                Application.dataPath,
                defaultName,
                extension
            );
        }

        private static bool ConfirmOverwrite(string filePath)
        {
            if (!File.Exists(filePath)) 
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Overwrite Existing File",
                $"A file already exists at:\n{filePath}\n\nDo you want to overwrite it?",
                "Overwrite",
                "Cancel"
            );
        }
    }
}
