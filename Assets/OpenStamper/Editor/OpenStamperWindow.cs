/*
 * Asset: OpenStamper
 * Filename: OpenStamperWindow.cs
 * Asset Version: 1.0
 * Unity Version: Unity 6+ (URP)
 * Author: DeathwatchGaming
 * License: MIT
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OpenHeightmap;

namespace OpenStamper
{
    public class OpenStamperWindow : EditorWindow
    {
        // ── Core ──
        private HeightmapData baseHeightmap;
        private HeightmapData workingHeightmap;
        [SerializeField] private StampStack stack = new StampStack();
        [SerializeField] private StampCompositor compositor = new StampCompositor();
        private PreviewRenderer previewRenderer;

        // The base heightmap is kept across script recompiles (stored flat, row-major).
        [SerializeField] private float[] baseHeightsFlat;
        [SerializeField] private int baseWidth;
        [SerializeField] private int baseHeight;
        [SerializeField] private bool stackApplied;

        // ── UI State ──
        private Vector2 scrollPos;
        private Vector2 previewScrollPos;
        [SerializeField] private int selectedTab;
        private string[] tabNames = { "Stamps", "Layer", "Libraries", "Export" };
        private string statusMessage = "Load a base heightmap to start stamping";
        [SerializeField] private float previewZoom = 1f;
        private Rect previewRect;

        // ── Preview cache ──
        private Texture2D cachedPreview;
        /// <summary>The heightmap the cached preview was rendered from.</summary>
        private HeightmapData cachedPreviewSource;

        // Single-layer preview shown instead of the working result (never exported or applied).
        private HeightmapData layerPreview;
        /// <summary>Name of the layer shown in the single-layer preview.</summary>
        private string layerPreviewName;

        // ── Selection ──
        [SerializeField] private int selectedLayerIndex = -1;

        // ── Libraries ──
        private List<StampLibrary> libraries = new List<StampLibrary>();
        private string[] libraryPresets = { "Impact Site", "Mountain Valley", "River Cut", "Mesa" };

        // ── Export ──
        [SerializeField] private OpenHeightmap.ExportFormat exportFormat = OpenHeightmap.ExportFormat.PNG;
        [SerializeField] private bool postProcessEnabled = false;
        [SerializeField] private int smoothRadius = 3;
        [SerializeField] private bool clampToRange = true;
        [SerializeField] private bool normalizeResult = false;
        [SerializeField] private NewTerrainSettings newTerrain = new NewTerrainSettings();

        /// <summary>
        /// Project folder where new TerrainData assets are saved. Kept outside the tool folder so
        /// updating or removing the tool never touches the user's terrains.
        /// </summary>
        private const string TerrainAssetFolder = "Assets/OpenStamper Generated";

        [MenuItem("Tools/OpenStamper %&s")]
        public static void ShowWindow()
        {
            var window = GetWindow<OpenStamperWindow>("OpenStamper");
            window.minSize = new Vector2(900, 650);
            window.Show();
        }

        private void OnEnable()
        {
            previewRenderer = new PreviewRenderer();

            if (stack == null)
            {
                stack = new StampStack();
            }

            if (compositor == null)
            {
                compositor = new StampCompositor();
            }

            if (newTerrain == null)
            {
                newTerrain = new NewTerrainSettings();
            }


            stack.EnsureUniqueIds();
            RefreshLibraries();
            RestoreBaseAfterReload();
        }

        private void OnDisable()
        {
            previewRenderer?.Cleanup();
        }

        /// <summary>Refreshes the saved library list when assets change in the project.</summary>
        private void OnProjectChange()
        {
            RefreshLibraries();
            Repaint();
        }

        /// <summary>
        /// Runs an action after the current GUI pass. Anything that opens a dialog or file panel, or
        /// creates assets/objects, must not run inside OnGUI's layout groups.
        /// </summary>
        private void Defer(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                action();
                Repaint();
            };
        }

        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidebar();
                DrawMainArea();
            }

            DrawStatusBar();
        }

        // ═══════════════════════════════════════════════════
        //  TOOLBAR
        // ═══════════════════════════════════════════════════

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Load Base", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    Defer(LoadBaseHeightmap);
                }

                if (GUILayout.Button("Load OpenHeightmap", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    Defer(LoadFromOpenHeightmap);
                }

                if (GUILayout.Button("Apply All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    Defer(ApplyStack);
                }

                if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(55)))
                {
                    ResetStack();
                }

                if (GUILayout.Button("Apply to Terrain", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    Defer(ApplyToTerrain);
                }

                GUILayout.Space(10);

                exportFormat = (OpenHeightmap.ExportFormat)EditorGUILayout.EnumPopup(exportFormat, GUILayout.Width(60));

                if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Defer(ExportResult);
                }

                GUILayout.FlexibleSpace();

                if (workingHeightmap != null)
                {
                    GUILayout.Label($"{workingHeightmap.Width}x{workingHeightmap.Height}", EditorStyles.miniLabel);
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  SIDEBAR
        // ═══════════════════════════════════════════════════

        private void DrawSidebar()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos, GUILayout.Width(300), GUILayout.ExpandHeight(true)))
            {
                scrollPos = scrollScope.scrollPosition;
                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
                GUILayout.Space(5);

                switch (selectedTab)
                {
                    case 0: DrawStampsTab(); break;
                    case 1: DrawLayerTab(); break;
                    case 2: DrawLibrariesTab(); break;
                    case 3: DrawExportTab(); break;
                }
            }
        }

        private void DrawStampsTab()
        {
            EditorGUILayout.LabelField("Stamp Stack", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click + to add a stamp as a new layer, then edit it in the Layer tab and click Apply All.", MessageType.Info);

            EditorGUILayout.Space();

            var names = StampProviderRegistry.Names;

            for (int i = 0; i < names.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                    {
                        var layer = stack.AddLayer(names[i]);
                        layer.StampTypeIndex = i;
                        selectedLayerIndex = stack.Layers.Count - 1;
                        statusMessage = $"Added: {names[i]} (click Apply All to see it)";
                    }

                    EditorGUILayout.LabelField($"{names[i]} ({GetStampDescription(i)})", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Layers: {stack.Layers.Count} ({stack.EnabledCount} enabled)", EditorStyles.miniLabel);
        }

        private void DrawLayerTab()
        {
            if (stack.Layers.Count == 0)
            {
                EditorGUILayout.HelpBox("No stamps in stack. Add stamps from the Stamps tab.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Layer Stack", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use ▲ ▼ to reorder (bottom of the list is applied last). Click a name to edit it.", MessageType.Info);

            EditorGUILayout.Space();

            // Layer list
            for (int i = 0; i < stack.Layers.Count; i++)
            {
                var layer = stack.Layers[i];
                bool isSelected = (i == selectedLayerIndex);

                using (new EditorGUILayout.VerticalScope(isSelected ? EditorStyles.helpBox : GUIStyle.none))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        layer.Enabled = GUILayout.Toggle(layer.Enabled, "", GUILayout.Width(20));

                        string typeName = layer.StampTypeIndex >= 0 && layer.StampTypeIndex < StampProviderRegistry.Names.Count
                            ? StampProviderRegistry.Names[layer.StampTypeIndex]
                            : "?";

                        string label = $"{layer.Name} [{typeName}]";

                        if (GUILayout.Toggle(isSelected, label, EditorStyles.boldLabel))
                        {
                            selectedLayerIndex = i;
                        }

                        GUILayout.FlexibleSpace();

                        // Move buttons
                        if (GUILayout.Button("▲", GUILayout.Width(22)) && i > 0)
                        {
                            stack.MoveLayer(layer.Id, -1);

                            // Keep the same layer selected after it moves
                            if (selectedLayerIndex == i)
                            {
                                selectedLayerIndex = i - 1;
                            }

                            else if (selectedLayerIndex == i - 1)
                            {
                                selectedLayerIndex = i;
                            }
                        }

                        if (GUILayout.Button("▼", GUILayout.Width(22)) && i < stack.Layers.Count - 1)
                        {
                            stack.MoveLayer(layer.Id, 1);

                            // Keep the same layer selected after it moves
                            if (selectedLayerIndex == i)
                            {
                                selectedLayerIndex = i + 1;
                            }

                            else if (selectedLayerIndex == i + 1)
                            {
                                selectedLayerIndex = i;
                            }
                        }

                        if (GUILayout.Button(new GUIContent("⎘", "Duplicate"), GUILayout.Width(22)))
                        {
                            stack.DuplicateLayer(layer.Id);
                            selectedLayerIndex = i + 1;
                            break;
                        }

                        if (GUILayout.Button("✖", GUILayout.Width(22)))
                        {
                            stack.RemoveLayer(layer.Id);

                            if (selectedLayerIndex >= stack.Layers.Count)
                            {
                                selectedLayerIndex = stack.Layers.Count - 1;
                            }

                            break;
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            // Selected layer properties
            if (selectedLayerIndex >= 0 && selectedLayerIndex < stack.Layers.Count)
            {
                var selected = stack.Layers[selectedLayerIndex];
                EditorGUILayout.LabelField("Layer Properties", EditorStyles.boldLabel);

                selected.Name = EditorGUILayout.TextField("Name", selected.Name);
                selected.StampTypeIndex = EditorGUILayout.Popup("Stamp Type", selected.StampTypeIndex, StampProviderRegistry.Names.ToArray());
                selected.BlendMode = (BlendMode)EditorGUILayout.EnumPopup("Blend Mode", selected.BlendMode);

                EditorGUILayout.Space();
                selected.Position = EditorGUILayout.Vector2Field("Position (UV)", selected.Position);
                selected.Radius = EditorGUILayout.Slider(new GUIContent("Radius", "Fraction of the terrain width from the stamp's center to its edge."), selected.Radius, 0.01f, 0.8f);
                selected.Rotation = EditorGUILayout.Slider("Rotation", selected.Rotation, 0f, 360f);
                selected.Intensity = EditorGUILayout.Slider(new GUIContent("Intensity", "Strength. Values above 1 only have an extra effect for Add and Subtract."), selected.Intensity, 0f, 2f);
                selected.FalloffPower = EditorGUILayout.Slider("Falloff Power", selected.FalloffPower, 0.5f, 8f);
                selected.VerticalScale = EditorGUILayout.Slider("Vertical Scale", selected.VerticalScale, 0.1f, 5f);
                selected.Invert = EditorGUILayout.Toggle(new GUIContent("Invert", "Flip the shape: raised features become pits, carved ones (craters, valleys, rivers) become raised."), selected.Invert);

                // Stamp-specific parameters (stored on this layer)
                var provider = selected.GetProvider();

                if (provider != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"{provider.Name} Settings", EditorStyles.boldLabel);

                    if (provider is CraterStamp crater)
                    {
                        crater.Depth = EditorGUILayout.Slider("Depth", crater.Depth, 0f, 1f);
                        crater.RimHeight = EditorGUILayout.Slider("Rim Height", crater.RimHeight, 0f, 1f);
                        crater.RimWidth = EditorGUILayout.Slider("Rim Width", crater.RimWidth, 0.01f, 0.5f);
                        crater.FloorFlatness = EditorGUILayout.Slider("Floor Flatness", crater.FloorFlatness, 0f, 1f);
                    }

                    else if (provider is MountainStamp mountain)
                    {
                        mountain.PeakHeight = EditorGUILayout.Slider("Peak Height", mountain.PeakHeight, 0f, 1f);
                        mountain.Steepness = EditorGUILayout.Slider("Steepness", mountain.Steepness, 1f, 10f);
                        mountain.BaseWidth = EditorGUILayout.Slider("Base Width", mountain.BaseWidth, 0.1f, 1f);
                        mountain.Erosion = EditorGUILayout.Slider("Erosion", mountain.Erosion, 0f, 1f);
                    }

                    else if (provider is RidgeStamp ridge)
                    {
                        ridge.Height = EditorGUILayout.Slider("Height", ridge.Height, 0f, 1f);
                        ridge.Width = EditorGUILayout.Slider("Width", ridge.Width, 0.01f, 0.5f);
                        ridge.Jaggedness = EditorGUILayout.Slider("Jaggedness", ridge.Jaggedness, 0f, 1f);
                        ridge.Branching = EditorGUILayout.Slider("Branching", ridge.Branching, 0f, 1f);
                    }

                    else if (provider is ValleyStamp valley)
                    {
                        valley.Depth = EditorGUILayout.Slider("Depth", valley.Depth, 0f, 1f);
                        valley.FloorWidth = EditorGUILayout.Slider("Floor Width", valley.FloorWidth, 0.01f, 0.5f);
                        valley.WallSteepness = EditorGUILayout.Slider("Wall Steepness", valley.WallSteepness, 1f, 10f);
                        valley.Winding = EditorGUILayout.Slider("Winding", valley.Winding, 0f, 1f);
                    }

                    else if (provider is PlateauStamp plateau)
                    {
                        plateau.Height = EditorGUILayout.Slider("Height", plateau.Height, 0f, 1f);
                        plateau.RimSharpness = EditorGUILayout.Slider("Rim Sharpness", plateau.RimSharpness, 1f, 10f);
                        plateau.SurfaceRoughness = EditorGUILayout.Slider("Surface Roughness", plateau.SurfaceRoughness, 0f, 1f);
                        plateau.Erosion = EditorGUILayout.Slider("Erosion", plateau.Erosion, 0f, 1f);
                    }

                    else if (provider is RiverStamp river)
                    {
                        river.Depth = EditorGUILayout.Slider("Depth", river.Depth, 0f, 1f);
                        river.Width = EditorGUILayout.Slider("Width", river.Width, 0.01f, 0.3f);
                        river.MeanderFrequency = EditorGUILayout.Slider("Meander Freq", river.MeanderFrequency, 0f, 1f);
                        river.MeanderAmplitude = EditorGUILayout.Slider("Meander Amp", river.MeanderAmplitude, 0f, 1f);
                        river.Seed = EditorGUILayout.IntField("Seed", river.Seed);
                    }

                    else if (provider is NoiseStamp noise)
                    {
                        noise.Frequency = EditorGUILayout.Slider("Frequency", noise.Frequency, 0.1f, 10f);
                        noise.Amplitude = EditorGUILayout.Slider("Amplitude", noise.Amplitude, 0f, 1f);
                        noise.Octaves = EditorGUILayout.IntSlider("Octaves", noise.Octaves, 1, 6);
                        noise.NoiseType = EditorGUILayout.Popup("Noise Type", noise.NoiseType, new string[] { "Perlin", "Value", "Billow", "Ridged" });
                        noise.Seed = EditorGUILayout.IntField("Seed", noise.Seed);
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Preview This Layer"))
                {
                    StampLayer toPreview = selected;
                    Defer(() => PreviewSingleLayer(toPreview));
                }
            }
        }

        private void DrawLibrariesTab()
        {
            EditorGUILayout.LabelField("Stamp Libraries", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Built-in Presets", EditorStyles.boldLabel);

            for (int i = 0; i < libraryPresets.Length; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(libraryPresets[i], EditorStyles.label);

                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        var tempLib = CreateInstance<StampLibrary>();
                        tempLib.LoadPreset(libraryPresets[i]);
                        stack = tempLib.Stack.Clone();
                        selectedLayerIndex = -1;
                        statusMessage = $"Loaded preset: {libraryPresets[i]} (click Apply All)";
                        DestroyImmediate(tempLib);
                    }
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Saved Libraries", EditorStyles.boldLabel);

                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    RefreshLibraries();
                }
            }

            for (int i = 0; i < libraries.Count; i++)
            {
                if (libraries[i] == null)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(libraries[i].name, EditorStyles.label);

                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        // Work on a copy so edits never change the saved library.
                        stack = (libraries[i].Stack ?? new StampStack()).Clone();
                        selectedLayerIndex = -1;
                        statusMessage = $"Loaded: {libraries[i].name} (click Apply All)";
                    }
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Current as Library"))
            {
                Defer(SaveLibrary);
            }
        }

        private void DrawExportTab()
        {
            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);

            if (workingHeightmap == null)
            {
                EditorGUILayout.HelpBox("No heightmap loaded. Use 'Load Base' or 'Load OpenHeightmap' first.", MessageType.Info);
            }

            else
            {
                EditorGUILayout.LabelField($"Current: {workingHeightmap.Width}x{workingHeightmap.Height}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            normalizeResult = EditorGUILayout.Toggle(
                new GUIContent("Normalize Result", "Stretch the final heights to fill 0-1. Off keeps the base terrain's heights and clips anything stamped above 1 or below 0."),
                normalizeResult);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Post-Processing", EditorStyles.boldLabel);
            postProcessEnabled = EditorGUILayout.Toggle("Enable Post-Processing", postProcessEnabled);

            if (postProcessEnabled)
            {
                EditorGUI.indentLevel++;
                clampToRange = EditorGUILayout.Toggle("Clamp to [0,1]", clampToRange);
                smoothRadius = EditorGUILayout.IntSlider("Smooth Radius", smoothRadius, 1, 20);
                EditorGUILayout.HelpBox(
                    $"Smooth radius {smoothRadius}: reduces high-frequency detail. " +
                    (smoothRadius <= 2 ? "Light smooth." : smoothRadius <= 5 ? "Moderate smooth." : "Heavy smooth — significant detail loss."),
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("New Terrain", EditorStyles.boldLabel);
            newTerrain.DrawGUI(workingHeightmap != null ? workingHeightmap.Width : 513);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(workingHeightmap == null))
            {
                if (GUILayout.Button("Export Heightmap"))
                {
                    Defer(ExportResult);
                }
            }

            EditorGUILayout.Space();
            compositor.StampResolution = EditorGUILayout.IntPopup("Stack Preview Res", compositor.StampResolution,
                new string[] { "129", "257", "513", "1025" },
                new int[] { 129, 257, 513, 1025 });

            if (GUILayout.Button("Export Stack Preview"))
            {
                Defer(ExportStackPreview);
            }
        }

        // ═══════════════════════════════════════════════════
        //  MAIN AREA
        // ═══════════════════════════════════════════════════

        private void DrawMainArea()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                HeightmapData shown = layerPreview ?? workingHeightmap;

                if (shown != null && shown.Heights != null)
                {
                    DrawPreviewArea(shown);
                }

                else
                {
                    DrawEmptyState();
                }
            }
        }

        /// <summary>Returns the cached preview, re-rendering only when the shown heightmap changed.</summary>
        private Texture2D GetPreviewTexture(HeightmapData shown)
        {
            if (cachedPreview == null || cachedPreviewSource != shown)
            {
                try
                {
                    cachedPreview = previewRenderer.RenderToTexture(shown);
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"[OpenStamper] Preview failed: {ex.Message}");
                    cachedPreview = null;
                }

                cachedPreviewSource = shown;
            }

            return cachedPreview;
        }

        /// <summary>Draws the preview image, zoom slider and (for layer previews) a way back to the result.</summary>
        private void DrawPreviewArea(HeightmapData shown)
        {
            if (layerPreview != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Layer preview: {layerPreviewName} (not applied; export and terrain use the full stack)", EditorStyles.miniBoldLabel);

                    if (GUILayout.Button("Back to Result", GUILayout.Width(110)))
                    {
                        // Deferred so this frame's layout stays consistent.
                        Defer(() => layerPreview = null);
                    }
                }
            }

            EditorGUILayout.LabelField($"Preview: {shown.Width}x{shown.Height} | Min: {shown.MinHeight:F4} | Max: {shown.MaxHeight:F4}", EditorStyles.miniLabel);

            previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, GUILayout.ExpandHeight(true));

            Texture2D previewTex = GetPreviewTexture(shown);

            if (previewTex != null)
            {
                float availableWidth = position.width - 320;
                float availableHeight = position.height - 100;
                float texAspect = (float)previewTex.width / previewTex.height;

                float displayWidth = availableWidth * previewZoom;
                float displayHeight = displayWidth / texAspect;

                if (displayHeight > availableHeight)
                {
                    displayHeight = availableHeight;
                    displayWidth = displayHeight * texAspect;
                }

                previewRect = GUILayoutUtility.GetRect(displayWidth, displayHeight);
                GUI.DrawTexture(previewRect, previewTex, ScaleMode.ScaleToFit, false);

                // Click to read the height under the cursor
                if (Event.current.type == EventType.MouseDown && previewRect.Contains(Event.current.mousePosition))
                {
                    float u = (Event.current.mousePosition.x - previewRect.x) / previewRect.width;
                    float v = 1f - (Event.current.mousePosition.y - previewRect.y) / previewRect.height;
                    u = Mathf.Clamp01(u);
                    v = Mathf.Clamp01(v);
                    float height = shown.GetHeightBilinear(u, v);
                    statusMessage = $"({u:F3}, {v:F3}) | Height: {height:F4}";
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Zoom:");
            previewZoom = EditorGUILayout.Slider(previewZoom, 0.2f, 3f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("Load a base heightmap ('Load Base' or 'Load OpenHeightmap') to begin.", MessageType.Info);
            GUILayout.FlexibleSpace();
        }

        // ═══════════════════════════════════════════════════
        //  STATUS BAR
        // ═══════════════════════════════════════════════════

        private void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(statusMessage, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (workingHeightmap != null)
                {
                    GUILayout.Label($"Layers: {stack.EnabledCount}/{stack.Layers.Count}", EditorStyles.miniLabel);
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════

        private void LoadBaseHeightmap()
        {
            string path = EditorUtility.OpenFilePanel("Load Heightmap", Application.dataPath, "png,raw,exr");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                byte[] fileData = File.ReadAllBytes(path);
                HeightmapData loaded = ext == ".raw" ? LoadRAW16(fileData) : LoadTexture(fileData, ext);

                SetBase(loaded);
                statusMessage = $"Loaded: {Path.GetFileName(path)} ({loaded.Width}x{loaded.Height})";
            }

            catch (Exception ex)
            {
                statusMessage = $"Load failed: {ex.Message}";
                Debug.LogError($"[OpenStamper] {ex.Message}");
                EditorUtility.DisplayDialog("Load Failed", ex.Message, "OK");
            }
        }

        /// <summary>Reads a square, little-endian 16-bit RAW heightmap.</summary>
        private HeightmapData LoadRAW16(byte[] data)
        {
            if (data.Length < 2)
            {
                throw new Exception("RAW file too small");
            }

            int pixelCount = data.Length / 2;
            int res = Mathf.RoundToInt(Mathf.Sqrt(pixelCount));

            if (res * res != pixelCount || data.Length % 2 != 0)
            {
                throw new Exception($"RAW file size ({data.Length} bytes) does not match a square 16-bit heightmap");
            }

            var heightmap = new HeightmapData(res);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int idx = (y * res + x) * 2;
                    ushort h16 = (ushort)(data[idx] | (data[idx + 1] << 8));
                    heightmap.Heights[x, y] = h16 / 65535f;
                }
            }

            heightmap.RecalculateBounds();
            return heightmap;
        }

        /// <summary>Reads a square PNG (or EXR, where Unity supports it) using its red channel.</summary>
        private HeightmapData LoadTexture(byte[] data, string ext)
        {
            Texture2D tex = new Texture2D(2, 2);

            try
            {
                if (!tex.LoadImage(data))
                {
                    throw new Exception(ext == ".exr"
                        ? "This Unity version couldn't read the EXR file. Export as RAW (16-bit) instead."
                        : "Couldn't read the image file.");
                }

                int w = tex.width;
                int h = tex.height;

                if (w != h)
                {
                    throw new Exception($"Heightmap images must be square (this one is {w}x{h}).");
                }

                // GetPixels returns floats, so 16-bit images keep their precision when Unity loads them as 16-bit.
                Color[] pixels = tex.GetPixels();
                var heightmap = new HeightmapData(w);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        heightmap.Heights[x, y] = pixels[y * w + x].r;
                    }
                }

                heightmap.RecalculateBounds();
                return heightmap;
            }

            finally
            {
                DestroyImmediate(tex);
            }
        }

        private void LoadFromOpenHeightmap()
        {
            var source = OpenHeightmapWindow.CurrentHeightmap;

            if (source == null || source.Heights == null)
            {
                EditorUtility.DisplayDialog("No Heightmap",
                    "No heightmap available in OpenHeightmap.\n\nOpen Tools > OpenHeightmap and generate one first.", "OK");
                return;
            }

            SetBase(source.Clone());
            // Keep the proportions chosen in OpenHeightmap (e.g. a preset's suggested height).
            newTerrain.HeightRatio = OpenHeightmapWindow.CurrentHeightRatio;
            statusMessage = $"Loaded from OpenHeightmap ({source.Width}x{source.Height})";
        }

        /// <summary>Sets a new base heightmap, resets the working result, and remembers it across reloads.</summary>
        private void SetBase(HeightmapData heightmap)
        {
            baseHeightmap = heightmap;
            workingHeightmap = heightmap.Clone();
            layerPreview = null;
            stackApplied = false;

            baseWidth = heightmap.Width;
            baseHeight = heightmap.Height;
            baseHeightsFlat = heightmap.ToFlatArray();
        }

        /// <summary>Rebuilds the base heightmap (and applied stack) after a script recompile cleared them.</summary>
        private void RestoreBaseAfterReload()
        {
            if (baseHeightmap != null || baseHeightsFlat == null || baseWidth <= 0 || baseHeight <= 0 ||
                baseHeightsFlat.Length != baseWidth * baseHeight)
            {
                return;
            }

            baseHeightmap = HeightmapData.FromFlatArray(baseHeightsFlat, baseWidth, baseHeight);
            workingHeightmap = baseHeightmap.Clone();

            if (stackApplied)
            {
                compositor.Composite(workingHeightmap, stack);
            }

            statusMessage = "Restored base heightmap after script reload";
        }

        private void ApplyStack()
        {
            if (baseHeightmap == null)
            {
                EditorUtility.DisplayDialog("No Base", "Load a base heightmap first.", "OK");
                return;
            }

            // Heights are kept as-is (no automatic normalize), so adding a stamp never rescales the
            // rest of the terrain. See "Normalize Result" in the Export tab.
            workingHeightmap = baseHeightmap.Clone();
            compositor.Composite(workingHeightmap, stack);
            layerPreview = null;
            stackApplied = true;
            statusMessage = $"Applied {stack.EnabledCount} stamp(s)";
        }

        private void PreviewSingleLayer(StampLayer layer)
        {
            if (baseHeightmap == null)
            {
                EditorUtility.DisplayDialog("No Base", "Load a base heightmap first.", "OK");
                return;
            }

            if (layer == null || layer.GetProvider() == null)
            {
                return;
            }

            layerPreview = compositor.PreviewSingle(baseHeightmap, layer);
            layerPreviewName = layer.Name;
            statusMessage = $"Previewing: {layer.Name}";
        }

        private void ResetStack()
        {
            stack = new StampStack();
            selectedLayerIndex = -1;
            layerPreview = null;
            stackApplied = false;

            if (baseHeightmap != null)
            {
                workingHeightmap = baseHeightmap.Clone();
            }

            statusMessage = "Stack reset";
            Repaint();
        }

        /// <summary>The final heights for export / terrain: optional normalize, post-processing, then 0-1.</summary>
        private HeightmapData GetOutputHeightmap()
        {
            if (workingHeightmap == null)
            {
                return null;
            }

            var output = workingHeightmap.Clone();

            if (normalizeResult)
            {
                output.Normalize();
            }

            if (postProcessEnabled)
            {
                if (clampToRange)
                {
                    output.Clamp(0f, 1f);
                }

                if (smoothRadius > 0)
                {
                    output.GaussianSmooth(smoothRadius);
                }
            }

            // Terrains and 16-bit files can only hold 0-1.
            output.Clamp(0f, 1f);
            return output;
        }

        /// <summary>Suffix for status messages when post-processing is on.</summary>
        private string PostProcessNote => postProcessEnabled ? " (post-processed)" : "";

        private void ExportResult()
        {
            var toExport = GetOutputHeightmap();

            if (toExport == null)
            {
                EditorUtility.DisplayDialog("Nothing to Export", "Load a base heightmap and apply stamps first.", "OK");
                return;
            }

            string ext = exportFormat switch
            {
                OpenHeightmap.ExportFormat.PNG => "png",
                OpenHeightmap.ExportFormat.RAW16 => "raw",
                OpenHeightmap.ExportFormat.EXR => "exr",
                _ => "png"
            };

            string path = StampExporter.GetExportPath("StampedHeightmap", ext);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            bool ok = StampExporter.ExportHeightmap(toExport, path, exportFormat);
            statusMessage = ok ? $"Exported: {path}{PostProcessNote}" : "Export failed (see Console)";
        }

        /// <summary>Exports an image of the stack alone (on a flat base) as PNG.</summary>
        private void ExportStackPreview()
        {
            string path = StampExporter.GetExportPath("StampPreview", "png");

            if (!string.IsNullOrEmpty(path))
            {
                bool ok = StampExporter.ExportStackPreview(stack, StampProviderRegistry.Providers, path, compositor.StampResolution);
                statusMessage = ok ? $"Stack preview exported: {path}" : "Export failed (see Console)";
            }
        }

        private void ApplyToTerrain()
        {
            var output = GetOutputHeightmap();

            if (output == null)
            {
                EditorUtility.DisplayDialog("No Heightmap", "Load a base heightmap and apply stamps first.", "OK");
                return;
            }

            string result = TerrainApplier.Apply(output, newTerrain, TerrainAssetFolder, "OpenStamper Terrain");

            if (result != null)
            {
                statusMessage = result + PostProcessNote;
            }
        }

        /// <summary>Saves a copy of the current stack as a StampLibrary asset.</summary>
        private void SaveLibrary()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Stamp Library", "NewStampLibrary", "asset", "Save stamp library"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var library = StampLibrary.Create(path);
                // Save a copy so later edits to the working stack don't change the saved library.
                library.Stack = stack.Clone();
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                RefreshLibraries();
                statusMessage = "Library saved";
            }
        }

        private string GetStampDescription(int index)
        {
            return index switch
            {
                0 => "Impact crater with rim",
                1 => "Conical mountain peak",
                2 => "Elongated ridge line",
                3 => "Valley/canyon cut",
                4 => "Flat-topped plateau",
                5 => "Meandering river",
                6 => "Generic noise shape",
                _ => ""
            };
        }

        private void RefreshLibraries()
        {
            libraries.Clear();
            string[] guids = AssetDatabase.FindAssets("t:StampLibrary");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var lib = AssetDatabase.LoadAssetAtPath<StampLibrary>(path);

                if (lib != null)
                {
                    libraries.Add(lib);
                }
            }
        }
    }
}
