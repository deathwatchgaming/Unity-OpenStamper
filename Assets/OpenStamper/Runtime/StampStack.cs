/*
 * Asset: OpenStamper
 * Filename: StampStack.cs
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
    /// A named collection of stamp layers. The core data structure for the stamp stack.
    /// Serializable for undo/redo and preset save/load.
    /// </summary>
    [Serializable]
    public class StampStack
    {
        /// <summary>Ordered list of stamp layers. Bottom-to-top compositing order.</summary>
        public System.Collections.Generic.List<StampLayer> Layers = new System.Collections.Generic.List<StampLayer>();

        /// <summary>Adds a new stamp layer and returns it.</summary>
        public StampLayer AddLayer(string name = "New Stamp")
        {
            var layer = new StampLayer(name);
            Layers.Add(layer);
            return layer;
        }

        /// <summary>Removes a layer by ID.</summary>
        public bool RemoveLayer(int layerId)
        {
            return Layers.RemoveAll(l => l.Id == layerId) > 0;
        }

        /// <summary>Gets a layer by ID.</summary>
        public StampLayer GetLayer(int layerId)
        {
            return Layers.Find(l => l.Id == layerId);
        }

        /// <summary>Moves a layer up or down in the stack (changes compositing order).</summary>
        public void MoveLayer(int layerId, int direction)
        {
            int index = Layers.FindIndex(l => l.Id == layerId);

            if (index < 0) 
            {
                return;
            }

            int newIndex = index + direction;

            if (newIndex < 0 || newIndex >= Layers.Count) 
            {
                return;
            }

            var temp = Layers[index];
            Layers[index] = Layers[newIndex];
            Layers[newIndex] = temp;
        }

        /// <summary>Duplicates a layer.</summary>
        public StampLayer DuplicateLayer(int layerId)
        {
            var source = GetLayer(layerId);

            if (source == null) 
            {
                return null;
            }

            var clone = source.Clone();
            clone.Name = source.Name + " (Copy)";
            Layers.Insert(Layers.IndexOf(source) + 1, clone);
            return clone;
        }

        /// <summary>
        /// Deep copy of the whole stack. Use this when loading from or saving to a StampLibrary so
        /// editing the working stack never changes the saved library (and vice versa).
        /// </summary>
        public StampStack Clone()
        {
            var clone = JsonUtility.FromJson<StampStack>(JsonUtility.ToJson(this)) ?? new StampStack();
            clone.EnsureUniqueIds();
            return clone;
        }

        /// <summary>Gives a fresh ID to any layer whose ID is missing or duplicated.</summary>
        public void EnsureUniqueIds()
        {
            var seen = new System.Collections.Generic.HashSet<int>();

            foreach (var layer in Layers)
            {
                if (layer == null)
                {
                    continue;
                }

                while (layer.Id == 0 || !seen.Add(layer.Id))
                {
                    layer.Id = StampLayer.NewId();
                }
            }
        }

        /// <summary>Returns the number of enabled layers.</summary>
        public int EnabledCount
        {
            get
            {
                int count = 0;

                foreach (var layer in Layers)
                {
                    if (layer.Enabled) 
                    {
                        count++;
                    }
                }
                
                return count;
            }
        }
    }
}
