/*
 * Asset: OpenStamper
 * Filename: CraterStamp.cs
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
    /// Circular crater with raised rim. The classic impact crater shape:
    /// flat or slightly raised rim, steep inner wall, flat or bowl-shaped floor.
    /// Parameters: Depth (0-1), Rim Height (0-1), Rim Width (0-1), Floor Flatness (0-1).
    /// </summary>
    [Serializable]
    public class CraterStamp : IStampProvider
    {
        public string Name => "Crater";

        /// <summary>Depth of the crater floor below the rim. 0 = flat, 1 = maximum depth.</summary>
        public float Depth = 0.6f;
        /// <summary>Height of the raised rim above base level. 0 = no rim, 1 = tall rim.</summary>
        public float RimHeight = 0.3f;
        /// <summary>Width of the rim as fraction of radius. 0 = thin, 1 = wide rim.</summary>
        public float RimWidth = 0.15f;
        /// <summary>Flatness of the crater floor. 0 = bowl shape, 1 = perfectly flat floor.</summary>
        public float FloorFlatness = 0.7f;

        /// <summary>
        /// Crater height relative to the surrounding ground: the floor sits Depth below ground,
        /// the inner wall rises back to ground level and the rim stands up to RimHeight above it.
        /// </summary>
        public float Sample(float u, float v)
        {
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // 0 to 1 from center to edge

            if (dist > 1f) 
            {
                return 0f;
            }

            float innerEdge = 1f - RimWidth;
            float floorRadius = innerEdge * (1f - FloorFlatness * 0.3f);

            // Floor
            if (dist < floorRadius)
            {
                float floorH = 1f - Depth;

                if (FloorFlatness > 0.5f)
                {
                    return floorH - 1f;
                }
                
                // Bowl shape
                float bowl = 1f - (1f - (dist / floorRadius)) * (1f - Depth);
                return Mathf.Lerp(floorH, bowl, 1f - FloorFlatness) - 1f;
            }

            // Inner wall (between floor and rim)
            if (dist < innerEdge)
            {
                float t = (dist - floorRadius) / (innerEdge - floorRadius);
                float floorH = 1f - Depth;
                return Mathf.Lerp(floorH, 1f, t) - 1f;
            }

            // Rim
            float rimT = (dist - innerEdge) / RimWidth;
            float rimCurve = Mathf.Sin(rimT * Mathf.PI);
            return Mathf.Lerp(1f, 1f + RimHeight, rimCurve) - 1f;
        }
    }
}
