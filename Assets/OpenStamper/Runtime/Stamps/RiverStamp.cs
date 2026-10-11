/*
 * Asset: OpenStamper
 * Filename: RiverStamp.cs
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
    /// Meandering river channel. Creates a sinuous depression that can cut through terrain.
    /// Parameters: Depth (0-1), Width (0-1), Meander Frequency (0-1), Meander Amplitude (0-1).
    /// </summary>
    [Serializable]
    public class RiverStamp : IStampProvider
    {
        public string Name => "River";

        /// <summary>Depth of the river channel. Range: 0-1.</summary>
        public float Depth = 0.5f;
        /// <summary>Width of the river. Range: 0-1.</summary>
        public float Width = 0.08f;
        /// <summary>How often the river meanders. Higher = more bends. Range: 0-1.</summary>
        public float MeanderFrequency = 0.5f;
        /// <summary>How far the river deviates from straight. Range: 0-1.</summary>
        public float MeanderAmplitude = 0.3f;
        /// <summary>Random seed for meander pattern.</summary>
        public int Seed = 0;

        private static float Hash(float x, int seed)
        {
            int ix = (int)Mathf.Floor(x);
            int h = ix * 374761393 + seed * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }

        private static float SmoothNoise(float x, int seed)
        {
            int ix = (int)Mathf.Floor(x);
            float fx = x - ix;
            float t = fx * fx * (3f - 2f * fx);
            return Mathf.Lerp(Hash(ix, seed), Hash(ix + 1, seed), t);
        }

        public float Sample(float u, float v)
        {
            // River flows along the v-axis with meandering
            float meanderX = SmoothNoise(v * MeanderFrequency * 10f, Seed) * MeanderAmplitude;
            float riverCenter = 0.5f + meanderX;

            float dx = u - riverCenter;
            float dist = Mathf.Abs(dx);
            float halfWidth = Width * 0.5f;

            if (dist > halfWidth) 
            {
                return 0f;
            }

            // River cross-section: deeper in center, shallower at banks
            float t = dist / halfWidth;
            float profile = 1f - t * t;

            // Vary depth along length
            float depthVar = Mathf.Lerp(0.7f, 1f, SmoothNoise(v * 5f, Seed + 100));

            // Negative = carved below the surrounding ground
            return -Mathf.Clamp01(profile * Depth * depthVar);
        }
    }
}
