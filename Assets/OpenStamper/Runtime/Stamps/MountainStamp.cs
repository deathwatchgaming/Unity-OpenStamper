/*
 * Asset: OpenStamper
 * Filename: MountainStamp.cs
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
    /// Conical mountain peak with configurable steepness and base width.
    /// Parameters: Peak Height (0-1), Steepness (1-10), Base Width (0-1), Erosion (0-1).
    /// </summary>
    [Serializable]
    public class MountainStamp : IStampProvider
    {
        public string Name => "Mountain";

        /// <summary>Height of the peak. 0 = flat, 1 = maximum height.</summary>
        public float PeakHeight = 0.8f;
        /// <summary>Steepness of the cone. Higher = sharper peak. Range: 1-10.</summary>
        public float Steepness = 1.6f;
        /// <summary>Width of the mountain base relative to stamp radius.</summary>
        public float BaseWidth = 1f;
        /// <summary>Erosion amount. Smooths the peak and creates ridges. Range: 0-1.</summary>
        public float Erosion = 0f;

        private static float Noise(float x, float y)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);
            float fx = x - ix;
            float fy = y - iy;

            float a = Hash2D(ix, iy);
            float b = Hash2D(ix + 1, iy);
            float c = Hash2D(ix, iy + 1);
            float d = Hash2D(ix + 1, iy + 1);

            float ux = fx * fx * (3f - 2f * fx);
            float uy = fy * fy * (3f - 2f * fy);

            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        private static float Hash2D(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }

        public float Sample(float u, float v)
        {
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

            float baseRadius = BaseWidth;

            if (dist > baseRadius) 
            {
                return 0f;
            }

            // Cone shape with power curve for steepness
            float t = 1f - (dist / baseRadius);
            float height = Mathf.Pow(t, Steepness) * PeakHeight;

            // Apply erosion noise
            if (Erosion > 0f)
            {
                float angle = Mathf.Atan2(dy, dx);
                float noiseVal = Noise(u * 8f, v * 8f);
                float erosionEffect = Mathf.Lerp(1f, noiseVal, Erosion * 0.5f);
                height *= erosionEffect;

                // Ridge carving
                float ridgeNoise = Mathf.Abs(noiseVal * 2f - 1f);
                height *= Mathf.Lerp(1f, ridgeNoise, Erosion * 0.3f);
            }

            return Mathf.Clamp01(height);
        }
    }
}
