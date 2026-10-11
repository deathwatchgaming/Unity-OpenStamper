/*
 * Asset: OpenStamper
 * Filename: IStampProvider.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

namespace OpenStamper
{
    /// <summary>
    /// Interface for procedural stamp shape generators. Each implementation produces
    /// a heightmap pattern in normalized [0,1] UV space that can be placed on terrain.
    /// </summary>
    public interface IStampProvider
    {
        /// <summary>Display name of the stamp type.</summary>
        string Name { get; }

        /// <summary>
        /// Samples the stamp height at normalized UV coordinates, relative to the surrounding ground:
        /// 0 = no change, positive = raised (up to about 1), negative = carved below ground (craters,
        /// valleys, rivers). With the Add blend mode every stamp then looks the way its name suggests,
        /// and Subtract is its exact opposite.
        /// </summary>
        /// <param name="u">Horizontal position in [0, 1] (center = 0.5).</param>
        /// <param name="v">Vertical position in [0, 1] (center = 0.5).</param>
        float Sample(float u, float v);
    }
}
