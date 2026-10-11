/*
 * Asset: OpenStamper
 * Filename: ValleyStamp.cs
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
    /// Valley or canyon cut. Creates a lowered channel through terrain with
    /// configurable wall steepness and floor shape. Good for river valleys, canyons, ravines.
    /// Parameters: Depth (0-1), Floor Width (0-1), Wall Steepness (1-10), Winding (0-1).
    /// </summary>
    [Serializable]
    public class ValleyStamp : IStampProvider
    {
        public string Name => "Valley";

        /// <summary>Depth of the valley below surrounding terrain. Range: 0-1.</summary>
        public float Depth = 0.6f;
        /// <summary>Width of the flat valley floor. Range: 0-1.</summary>
        public float FloorWidth = 0.2f;
        /// <summary>Steepness of the valley walls. Higher = more cliff-like. Range: 1-10.</summary>
        public float WallSteepness = 3f;
        /// <summary>How much the valley winds. 0 = straight, 1 = very curvy. Range: 0-1.</summary>
        public float Winding = 0.3f;

        private static float Hash(float x, float y)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);
            int h = ix * 374761393 + iy * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }

        private static float SmoothNoise(float x)
        {
            int ix = (int)Mathf.Floor(x);
            float fx = x - ix;
            float t = fx * fx * (3f - 2f * fx);
            return Mathf.Lerp(Hash(ix, 0), Hash(ix + 1, 0), t);
        }

        public float Sample(float u, float v)
        {
            // Valley runs along the v-axis with winding
            float centerOffset = 0f;

            if (Winding > 0.01f)
            {
                centerOffset = (SmoothNoise(v * 6f) - 0.5f) * Winding * 0.4f;
            }

            float dx = u - (0.5f + centerOffset);
            float dist = Mathf.Abs(dx);

            // Valley cross-section
            float halfFloor = FloorWidth * 0.5f;

            if (dist < halfFloor)
            {
                // Flat floor (negative = carved below the surrounding ground)
                return -Depth;
            }

            // Walls
            float wallDist = (dist - halfFloor) / (0.5f - halfFloor);

            if (wallDist > 1f) 
            {
                return 0f;
            }

            float wallProfile = Mathf.Pow(wallDist, 1f / WallSteepness);
            return -Mathf.Clamp01(Depth * (1f - wallProfile));
        }
    }
}
