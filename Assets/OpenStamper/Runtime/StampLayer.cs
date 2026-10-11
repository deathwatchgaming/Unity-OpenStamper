/*
 * Asset: OpenStamper
 * Filename: StampLayer.cs
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
    /// Defines a single stamp instance placed on terrain. Contains the stamp shape and its own
    /// shape settings, placement transform and blend parameters.
    /// Serializable for undo/redo and preset save/load.
    /// </summary>
    [Serializable]
    public class StampLayer
    {
        /// <summary>Display name for this stamp layer.</summary>
        public string Name = "Stamp";

        /// <summary>Unique ID for tracking in the stamp stack.</summary>
        public int Id;

        /// <summary>Whether this layer is enabled (applied during composition).</summary>
        public bool Enabled = true;

        /// <summary>Index of the stamp type (see StampProviderRegistry.Names).</summary>
        public int StampTypeIndex;

        /// <summary>Normalized position on the terrain [0, 1]. (0.5, 0.5) = center.</summary>
        public Vector2 Position = new Vector2(0.5f, 0.5f);

        /// <summary>Stamp radius as a fraction of the terrain width. 0.5 = reaches the terrain edges from the center.</summary>
        public float Radius = 0.2f;

        /// <summary>Rotation angle in degrees.</summary>
        public float Rotation = 0f;

        /// <summary>Strength of the stamp. 1 = full effect; up to 2 for Add/Subtract.</summary>
        public float Intensity = 1f;

        /// <summary>How this stamp blends with existing terrain.</summary>
        public BlendMode BlendMode = BlendMode.Add;

        /// <summary>Edge falloff exponent. Higher = the stamp keeps full strength closer to its edge.</summary>
        public float FalloffPower = 2f;

        /// <summary>Flip the stamp shape: raised becomes carved and carved becomes raised (inside the stamp only).</summary>
        public bool Invert = false;

        /// <summary>Vertical stretch factor. > 1 = taller, < 1 = flatter.</summary>
        public float VerticalScale = 1f;

        // ── Per-layer shape settings (one set per stamp type, so two Crater layers can differ) ──
        /// <summary>This layer's Crater settings (used when Stamp Type is Crater).</summary>
        public CraterStamp Crater = new CraterStamp();
        /// <summary>This layer's Mountain settings (used when Stamp Type is Mountain).</summary>
        public MountainStamp Mountain = new MountainStamp();
        /// <summary>This layer's Ridge settings (used when Stamp Type is Ridge).</summary>
        public RidgeStamp Ridge = new RidgeStamp();
        /// <summary>This layer's Valley settings (used when Stamp Type is Valley).</summary>
        public ValleyStamp Valley = new ValleyStamp();
        /// <summary>This layer's Plateau settings (used when Stamp Type is Plateau).</summary>
        public PlateauStamp Plateau = new PlateauStamp();
        /// <summary>This layer's River settings (used when Stamp Type is River).</summary>
        public RiverStamp River = new RiverStamp();
        /// <summary>This layer's Noise settings (used when Stamp Type is Noise).</summary>
        public NoiseStamp Noise = new NoiseStamp();

        public StampLayer()
        {
            Id = NewId();
        }

        public StampLayer(string name)
        {
            Id = NewId();
            Name = name;
        }

        /// <summary>A new random ID. Random (rather than a counter) so IDs never repeat across script reloads.</summary>
        public static int NewId()
        {
            return Guid.NewGuid().GetHashCode() & 0x7fffffff;
        }

        /// <summary>This layer's shape settings for its current stamp type, or null for an unknown type.</summary>
        public IStampProvider GetProvider()
        {
            switch (StampTypeIndex)
            {
                case 0: return Crater ??= new CraterStamp();
                case 1: return Mountain ??= new MountainStamp();
                case 2: return Ridge ??= new RidgeStamp();
                case 3: return Valley ??= new ValleyStamp();
                case 4: return Plateau ??= new PlateauStamp();
                case 5: return River ??= new RiverStamp();
                case 6: return Noise ??= new NoiseStamp();
                default: return null;
            }
        }

        /// <summary>
        /// Evaluates the stamp at a terrain UV.
        /// value = the stamp's shape height (after Invert and Vertical Scale),
        /// mask = how much of the terrain is covered here (1 at the center, 0 at and beyond the radius).
        /// Returns false when the point is outside the stamp.
        /// </summary>
        public bool Evaluate(IStampProvider provider, float u, float v, out float value, out float mask)
        {
            value = 0f;
            mask = 0f;

            if (provider == null || Radius <= 0f)
            {
                return false;
            }

            float cosR = Mathf.Cos(Rotation * Mathf.Deg2Rad);
            float sinR = Mathf.Sin(Rotation * Mathf.Deg2Rad);

            // Translate to stamp center, then rotate
            float dx = u - Position.x;
            float dy = v - Position.y;
            float rdx = dx * cosR - dy * sinR;
            float rdy = dx * sinR + dy * cosR;

            float dist = Mathf.Sqrt(rdx * rdx + rdy * rdy) / Radius;

            if (dist >= 1f)
            {
                return false;
            }

            // Stamp shape space: [0,1] square spanning the full diameter.
            float su = rdx / (2f * Radius) + 0.5f;
            float sv = rdy / (2f * Radius) + 0.5f;

            float shape = provider.Sample(su, sv);

            if (Invert)
            {
                // Flip the shape: raised features become pits and carved ones become ridges.
                shape = -shape;
            }

            value = shape * VerticalScale;
            mask = 1f - Mathf.Pow(dist, Mathf.Max(0.01f, FalloffPower));
            return mask > 0f;
        }

        /// <summary>
        /// Generates a stamp map (value x mask) covering the whole terrain at the given resolution.
        /// Used for previews/exports; compositing samples the stamp directly at terrain resolution.
        /// </summary>
        public float[,] GenerateStampMap(IStampProvider provider, int resolution)
        {
            float[,] map = new float[resolution, resolution];

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);

                    if (Evaluate(provider, u, v, out float value, out float mask))
                    {
                        map[x, y] = value * mask * Intensity;
                    }
                }
            }

            return map;
        }

        /// <summary>Deep copy with a new ID.</summary>
        public StampLayer Clone()
        {
            var clone = JsonUtility.FromJson<StampLayer>(JsonUtility.ToJson(this));
            clone.Id = NewId();
            return clone;
        }
    }
}
