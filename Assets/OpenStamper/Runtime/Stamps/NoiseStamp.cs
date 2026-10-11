/*
 * Asset: OpenStamper
 * Filename: NoiseStamp.cs
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
    /// Generic noise-based stamp. Produces organic, varied terrain features
    /// using configurable noise parameters. A catch-all for custom shapes.
    /// Parameters: Frequency (0.1-10), Amplitude (0-1), Octaves (1-6), Noise Type (0-3).
    /// </summary>
    [Serializable]
    public class NoiseStamp : IStampProvider
    {
        public string Name => "Noise";

        /// <summary>Noise frequency. Higher = finer detail. Range: 0.1-10.</summary>
        public float Frequency = 2f;
        /// <summary>Noise amplitude. Range: 0-1.</summary>
        public float Amplitude = 0.5f;
        /// <summary>Number of noise octaves. Range: 1-6.</summary>
        public int Octaves = 3;
        /// <summary>Noise type: 0=Perlin, 1=Value, 2=Billow, 3=Ridged.</summary>
        public int NoiseType = 0;
        /// <summary>Seed for noise generation.</summary>
        public int Seed = 0;

        private static float Hash2D(int x, int y, int seed)
        {
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }

        private static float ValueNoise(float x, float y, int seed)
        {
            int ix = (int)Mathf.Floor(x);
            int iy = (int)Mathf.Floor(y);
            float fx = x - ix;
            float fy = y - iy;
            float ux = fx * fx * (3f - 2f * fx);
            float uy = fy * fy * (3f - 2f * fy);

            float a = Hash2D(ix, iy, seed);
            float b = Hash2D(ix + 1, iy, seed);
            float c = Hash2D(ix, iy + 1, seed);
            float d = Hash2D(ix + 1, iy + 1, seed);

            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        private static float RidgedNoise(float x, float y, int seed)
        {
            return 1f - Mathf.Abs(ValueNoise(x, y, seed) * 2f - 1f);
        }

        /// <summary>Gradient noise used by the Perlin noise type.</summary>
        private static readonly OpenHeightmap.PerlinNoise Perlin = new OpenHeightmap.PerlinNoise();

        private static float SampleNoise(float x, float y, int noiseType, int seed)
        {
            return noiseType switch
            {
                1 => ValueNoise(x, y, seed),
                2 => Mathf.Abs(ValueNoise(x, y, seed) * 2f - 1f),
                3 => RidgedNoise(x, y, seed),
                // Real gradient (Perlin) noise from OpenHeightmap, remapped from [-1,1] to [0,1].
                _ => Mathf.Clamp01((Perlin.Sample(x, y) + 1f) * 0.5f)
            };
        }

        public float Sample(float u, float v)
        {
            float amplitude = 1f;
            float frequency = Frequency;
            float noiseHeight = 0f;
            float maxAmplitude = 0f;

            for (int o = 0; o < Octaves; o++)
            {
                float sampleX = u * frequency + Seed + o * 31.7f;
                float sampleY = v * frequency + Seed + o * 47.3f;

                float val = SampleNoise(sampleX, sampleY, NoiseType, Seed + o * 1000);
                noiseHeight += val * amplitude;
                maxAmplitude += amplitude;

                amplitude *= 0.5f;
                frequency *= 2f;
            }

            float normalized = noiseHeight / maxAmplitude;

            // Apply circular falloff
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float falloff = 1f - Mathf.Pow(Mathf.Clamp01(dist), 2f);

            return Mathf.Clamp01(normalized * Amplitude * falloff);
        }
    }
}
