/*
 * Asset: OpenStamper
 * Filename: BlendMode.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

namespace OpenStamper
{
    /// <summary>
    /// How a stamp blends with existing terrain heights. Every mode only affects terrain inside the
    /// stamp's radius: final = lerp(terrain, result, coverage * intensity), where coverage fades
    /// from 1 at the center to 0 at the edge.
    /// </summary>
    public enum BlendMode
    {
        /// <summary>Raise terrain by the stamp: result = terrain + stamp</summary>
        Add,
        /// <summary>Lower terrain by the stamp: result = terrain - stamp</summary>
        Subtract,
        /// <summary>Scale terrain by the stamp: result = terrain * stamp</summary>
        Multiply,
        /// <summary>Keep the higher of terrain and stamp: result = max(terrain, stamp)</summary>
        Max,
        /// <summary>Keep the lower of terrain and stamp: result = min(terrain, stamp)</summary>
        Min,
        /// <summary>Halfway between terrain and stamp: result = (terrain + stamp) / 2</summary>
        Average,
        /// <summary>Replace terrain with the stamp: result = stamp</summary>
        Replace,
        /// <summary>Power curve: result = terrain ^ (1 - stamp)</summary>
        Power
    }
}
