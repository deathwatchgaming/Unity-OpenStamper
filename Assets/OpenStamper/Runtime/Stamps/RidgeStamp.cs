/*
 * Asset: OpenStamper
 * Filename: Ridgestamp.cs
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
    /// Elongated ridge line. Creates a raised linear feature with configurable
    /// width, height, and jaggedness. Good for mountain ridges, cliff edges, spines.
    /// Parameters: Height (0-1), Width (0-1), Jaggedness (0-1), Branching (0-1).
    /// </summary>
    [Serializable]
    public class RidgeStamp : IStampProvider
    {
        public string Name => "Ridge";

        /// <summary>Height of the ridge. Range: 0-1.</summary>
        public float Height = 0.7f;
        /// <summary>Width of the ridge base. Range: 0-1.</summary>
        public float Width = 0.15f;
        /// <summary>How jagged/irregular the ridge line is. Range: 0-1.</summary>
        public float Jaggedness = 0.3f;
        /// <summary>Branching probability for secondary ridges. Range: 0-1.</summary>
        public float Branching = 0.2f;

        private static float Hash(float x, float y)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);
            int h = ix * 374761393 + iy * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }

        private static float Noise1D(float x)
        {
            int ix = (int)Mathf.Floor(x);
            float fx = x - ix;
            float a = Hash(ix, 0);
            float b = Hash(ix + 1, 0);
            float t = fx * fx * (3f - 2f * fx);
            return Mathf.Lerp(a, b, t);
        }

        /// <summary>
        /// Ridge height at a stamp UV. The main ridge runs along v through the centre; branches are
        /// smaller side ridges that drift sideways and fade in and out smoothly along the length.
        /// </summary>
        public float Sample(float u, float v)
        {
            // Main ridge runs along the v-axis (vertical center)
            float main = RidgeProfile(Mathf.Abs(u - 0.5f), Width);

            // Branches: a secondary ridge whose position wanders smoothly, present only where
            // a slow noise value exceeds (1 - Branching), with a soft fade at its ends.
            float branch = 0f;

            if (Branching > 0.01f)
            {
                float presence = Mathf.Clamp01((Noise1D(v * 2f + 47.1f) - (1f - Branching)) * 5f);

                if (presence > 0f)
                {
                    float branchCenter = 0.5f + (Noise1D(v * 2.5f + 13.7f) - 0.5f) * 0.7f;
                    branch = RidgeProfile(Mathf.Abs(u - branchCenter), Width * 0.6f) * 0.7f * presence;
                }
            }

            float profile = Mathf.Max(main, branch);

            if (profile <= 0f)
            {
                return 0f;
            }

            // Jaggedness along the length
            float jagged = 1f;
            
            if (Jaggedness > 0.01f)
            {
                float noiseVal = Noise1D(v * 12f);
                jagged = Mathf.Lerp(1f, noiseVal, Jaggedness);
            }

            return Mathf.Clamp01(profile * jagged * Height);
        }

        /// <summary>Ridge cross-section: 1 at the crest, falling to 0 at halfWidth, with steep sides.</summary>
        private static float RidgeProfile(float distance, float halfWidth)
        {
            if (halfWidth <= 0f)
            {
                return 0f;
            }

            float t = distance / halfWidth;

            if (t >= 1f)
            {
                return 0f;
            }

            return 1f - Mathf.Pow(t, 0.75f);
        }
    }
}
