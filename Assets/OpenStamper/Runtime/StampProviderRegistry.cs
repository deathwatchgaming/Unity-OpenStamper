/*
 * Asset: OpenStamper
 * Filename: StampProviderRegistry.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System.Collections.Generic;
using UnityEngine;
using OpenHeightmap;

namespace OpenStamper
{
    /// <summary>
    /// Registry of the built-in stamp types: their names, order (StampTypeIndex) and default settings.
    /// Each StampLayer keeps its own copy of the settings (see StampLayer.GetProvider), so editing
    /// one layer never changes another. To add a stamp type, add it here and in StampLayer.
    /// </summary>
    public static class StampProviderRegistry
    {
        private static List<IStampProvider> providers;
        private static List<string> names;

        public static List<IStampProvider> Providers
        {
            get
            {
                if (providers == null) 
                {
                    Initialize();
                }

                return providers;
            }
        }

        public static List<string> Names
        {
            get
            {
                if (names == null) 
                {
                    Initialize();
                }

                return names;
            }
        }

        public static int Count => Providers.Count;

        private static void Initialize()
        {
            providers = new List<IStampProvider>
            {
                new CraterStamp(),
                new MountainStamp(),
                new RidgeStamp(),
                new ValleyStamp(),
                new PlateauStamp(),
                new RiverStamp(),
                new NoiseStamp()
            };

            names = new List<string>();

            foreach (var p in providers)
            {
                names.Add(p.Name);
            }
        }

        public static IStampProvider GetProvider(int index)
        {
            if (index < 0 || index >= Providers.Count) 
            {
                return null;
            }
            
            return Providers[index];
        }

        public static int GetIndex(string name)
        {
            return Names.IndexOf(name);
        }
    }
}
