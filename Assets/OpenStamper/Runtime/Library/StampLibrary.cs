/*
 * Asset: OpenStamper
 * Filename: StampLibrary.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenStamper
{
    /// <summary>
    /// ScriptableObject that stores a named collection of stamp layers.
    /// Created via Assets > Create > OpenStamper > Stamp Library.
    /// Used for saving/loading stamp configurations and sharing between projects.
    /// </summary>
    [CreateAssetMenu(fileName = "StampLibrary", menuName = "OpenStamper/Stamp Library", order = 200)]
    public class StampLibrary : ScriptableObject
    {
        /// <summary>The stamp stack stored in this library.</summary>
        public StampStack Stack = new StampStack();

        /// <summary>Description of this stamp library.</summary>
        [TextArea(3, 6)]
        public string Description = "";

        /// <summary>Tags for organizing libraries.</summary>
        public List<string> Tags = new List<string>();

        /// <summary>Creates a new StampLibrary asset at the given path.</summary>
        public static StampLibrary Create(string assetPath)
        {
            var library = CreateInstance<StampLibrary>();
            library.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(library, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
#endif

            return library;
        }

        /// <summary>
        /// Populates the stack with a preset set of stamps. All presets use the Add blend mode:
        /// carving stamps (Crater, Valley, River) go below ground on their own, so Add digs them in.
        /// Each preset was checked by rendering it on a gentle hilly base.
        /// </summary>
        public void LoadPreset(string presetName)
        {
            Stack.Layers.Clear();

            switch (presetName)
            {
                case "Impact Site":
                {
                    // One large crater: a sunken floor ringed by a raised rim.
                    var crater = new StampLayer("Crater");
                    crater.StampTypeIndex = 0; // CraterStamp
                    crater.BlendMode = BlendMode.Add;
                    crater.Radius = 0.2f;
                    crater.Intensity = 0.5f;
                    Stack.Layers.Add(crater);
                    break;
                }

                case "Mountain Valley":
                {
                    // Two long ridges with a valley carved between them.
                    var leftRidge = new StampLayer("Left Ridge");
                    leftRidge.StampTypeIndex = 2; // RidgeStamp
                    leftRidge.Position = new Vector2(0.28f, 0.5f);
                    leftRidge.Radius = 0.35f;
                    leftRidge.Rotation = 4f;
                    leftRidge.Intensity = 0.8f;
                    leftRidge.Ridge.Width = 0.25f;
                    Stack.Layers.Add(leftRidge);

                    var rightRidge = new StampLayer("Right Ridge");
                    rightRidge.StampTypeIndex = 2; // RidgeStamp
                    rightRidge.Position = new Vector2(0.72f, 0.5f);
                    rightRidge.Radius = 0.35f;
                    rightRidge.Rotation = -4f;
                    rightRidge.Intensity = 0.8f;
                    rightRidge.Ridge.Width = 0.25f;
                    Stack.Layers.Add(rightRidge);

                    var valley = new StampLayer("Central Valley");
                    valley.StampTypeIndex = 3; // ValleyStamp
                    valley.BlendMode = BlendMode.Add;
                    valley.Position = new Vector2(0.5f, 0.5f);
                    valley.Radius = 0.35f;
                    valley.Intensity = 0.4f;
                    Stack.Layers.Add(valley);
                    break;
                }

                case "River Cut":
                {
                    // A meandering river channel across most of the terrain.
                    var river = new StampLayer("Main River");
                    river.StampTypeIndex = 5; // RiverStamp
                    river.BlendMode = BlendMode.Add;
                    river.Radius = 0.48f;
                    river.Intensity = 0.6f;
                    river.FalloffPower = 6f;
                    Stack.Layers.Add(river);
                    break;
                }

                case "Mesa":
                {
                    // A broad flat-topped mesa with a weathered, pitted top.
                    var mesa = new StampLayer("Plateau");
                    mesa.StampTypeIndex = 4; // PlateauStamp
                    mesa.BlendMode = BlendMode.Add;
                    mesa.Radius = 0.25f;
                    mesa.Intensity = 0.7f;
                    mesa.FalloffPower = 6f;
                    Stack.Layers.Add(mesa);

                    var erosion = new StampLayer("Erosion Noise");
                    erosion.StampTypeIndex = 6; // NoiseStamp
                    erosion.BlendMode = BlendMode.Subtract;
                    erosion.Radius = 0.25f;
                    erosion.Intensity = 0.15f;
                    erosion.Noise.Frequency = 6f;
                    Stack.Layers.Add(erosion);
                    break;
                }
            }
        }
    }
}
