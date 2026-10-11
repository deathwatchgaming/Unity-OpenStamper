/*
 * Asset: OpenStamper
 * Filename: PlateauStamp.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System;
using UnityEngine;

namespace OpenStamper
{
    /// <summary>
    /// Flat-topped elevated area. Creates a mesa/butte shape with configurable
    /// rim sharpness and surface roughness. Good for plateaus, mesas, tablelands.
    /// Parameters: Height (0-1), Rim Sharpness (1-10), Surface Roughness (0-1), Erosion (0-1).
    /// </summary>
    [Serializable]
    public class PlateauStamp : IStampProvider
    {
        public string Name => "Plateau";

        /// <summary>Height of the plateau. Range: 0-1.</summary>
        public float Height = 0.6f;
        /// <summary>How sharp the rim transition is. Higher = more cliff-like. Range: 1-10.</summary>
        public float RimSharpness = 4f;
        /// <summary>Surface roughness on top of the plateau. Range: 0-1.</summary>
        public float SurfaceRoughness = 0.1f;
        /// <summary>Erosion on the rim edges. Range: 0-1.</summary>
        public float Erosion = 0f;

        private static float Hash(float x, float y)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);
            int h = ix * 374761393 + iy * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }

        private static float Noise(float x, float y)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);
            float fx = x - ix;
            float fy = y - iy;
            float ux = fx * fx * (3f - 2f * fx);
            float uy = fy * fy * (3f - 2f * fy);

            float a = Hash(ix, iy);
            float b = Hash(ix + 1, iy);
            float c = Hash(ix, iy + 1);
            float d = Hash(ix + 1, iy + 1);

            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        public float Sample(float u, float v)
        {
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

            if (dist > 1f) 
            {
                return 0f;
            }

            // Plateau profile: flat top, sharp rim, smooth base
            float plateauRadius = 0.5f;
            float rimWidth = 0.1f * (1f / RimSharpness);

            float profile;

            if (dist < plateauRadius - rimWidth)
            {
                // Flat top
                profile = 1f;

                // Surface roughness
                if (SurfaceRoughness > 0.01f)
                {
                    float roughness = Noise(u * 16f, v * 16f);
                    profile *= Mathf.Lerp(1f, roughness, SurfaceRoughness * 0.2f);
                }
            }

            else if (dist < plateauRadius + rimWidth)
            {
                // Rim transition
                float t = (dist - (plateauRadius - rimWidth)) / (rimWidth * 2f);
                profile = 1f - Mathf.Pow(t, RimSharpness);

                // Rim erosion
                if (Erosion > 0.01f)
                {
                    float erosionNoise = Hash(Mathf.Floor(u * 8f), Mathf.Floor(v * 8f));
                    profile *= Mathf.Lerp(1f, erosionNoise, Erosion * 0.4f);
                }
            }
            
            else
            {
                // Base slope
                float baseDist = (dist - plateauRadius) / (1f - plateauRadius);
                profile = Mathf.Pow(1f - baseDist, 2f);
            }

            return Mathf.Clamp01(profile * Height);
        }
    }
}
