# OpenStamper

A free Unity editor tool for stamping terrain features onto a heightmap. Stack craters, mountains, ridges, valleys, plateaus, rivers and noise as layers, preview the result live, then export it or apply it to a scene terrain.

![Mountain Valley](Previews/Stamp%20-%20Mountain%20Valley.png)

> **Requires [OpenHeightmap](https://github.com/deathwatchgaming/Unity-OpenHeightmap).** Install it first.

## Features

- **7 stamp types:** Crater, Mountain, Ridge, Valley, Plateau, River and Noise
- **8 blend modes:** Add, Subtract, Multiply, Max, Min, Average, Replace and Power
- **Layer stack:** reorder, toggle and preview layers one at a time, each with its own settings
- **4 preset libraries:** Impact Site, Mountain Valley, River Cut and Mesa; save your own as ScriptableObjects
- **Apply to Terrain:** updates the scene terrain (with undo), or creates a new one
- **Import and export:** 16-bit PNG, RAW16 or EXR

## Requirements

- Unity 6 or newer (URP)
- [OpenHeightmap](https://github.com/deathwatchgaming/Unity-OpenHeightmap)

## Installation

1. Install OpenHeightmap.
2. Download or clone this repository.
3. Copy the `Assets/OpenStamper` folder into your project's `Assets` folder.
4. Open the tool from **Tools > OpenStamper** (Ctrl+Alt+S).

## Quick Start

1. Click **Load OpenHeightmap** to bring in the current heightmap, or **Load Base** to import one from a file.
2. Add stamps in the **Stamps** tab and adjust them in the **Layer** tab.
3. Click **Apply All**.
4. Click **Apply to Terrain**, or **Export** to save the result.

Generated terrains are saved to `Assets/OpenStamper Generated`, outside the tool folder, so updating the tool never touches them.

## Documentation

Full documentation, covering every stamp, blend mode, preset and the API, is included with the tool:
`Assets/OpenStamper/Documentation/Index.html` (open it in your browser).

- [Read Online](https://deathwatchgaming.github.io/Unity-OpenStamper/Assets/OpenStamper/Documentation/Index.html)

## Previews

See the [Previews](https://github.com/deathwatchgaming/Unity-OpenStamper/tree/main/Previews) folder for a rendered image of every preset.

## License

MIT. See [LICENSE](LICENSE).
