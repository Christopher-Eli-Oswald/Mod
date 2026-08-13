using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mafi;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using UnityEngine;

namespace DeepMineMod;

/// <summary>
/// Live-save deep resource painter.
///
/// The brush rewrites only the material layer stack of each painted tile. It keeps the
/// tile's height, surface and flags unchanged, so a resource can be buried at a chosen
/// depth without raising/lowering the developed surface.
///
/// Hold LMB and drag to paint. RMB exits.
/// Shift + wheel = radius, Ctrl + wheel = deposit thickness, Alt + wheel = depth.
/// </summary>
[GlobalDependency(RegistrationMode.AsEverything, false, false)]
public sealed class DeepMineBrushTool : IUnityInputController {
    private const int MinRadius = 1;
    private const int MaxRadius = 100;
    private const int MinThickness = 1;
    private const int MaxThickness = 100;
    private const int MinDepth = 1;
    private const int MaxDepth = 300;
    private const int MaxSlicesPerTile = 1024;

    private readonly IUnityInputMgr m_inputManager;
    private readonly TerrainCursor m_terrainCursor;
    private readonly TerrainManager m_terrainManager;
    private readonly TerrainMaterialProto[] m_materials;
    private readonly MethodInfo m_setTileDataNoEvents;

    private int m_materialIndex;
    private int m_radius = 8;
    private int m_thickness = 10;
    private int m_depth = 50;
    private bool m_isActive;
    private bool m_hasLastPaintCenter;
    private Tile2i m_lastPaintCenter;

    public ControllerConfig Config => ControllerConfig.ToolBlockingCamera;

    public TerrainMaterialProto[] Materials => m_materials;
    public TerrainMaterialProto SelectedMaterial =>
        m_materials.Length == 0 ? null : m_materials[m_materialIndex];

    public int Radius => m_radius;
    public int Thickness => m_thickness;
    public int Depth => m_depth;

    public DeepMineBrushTool(
        IUnityInputMgr inputManager,
        TerrainCursor terrainCursor,
        TerrainManager terrainManager,
        ProtosDb protosDb)
    {
        m_inputManager = inputManager;
        m_terrainCursor = terrainCursor;
        m_terrainManager = terrainManager;

        m_materials = protosDb.All<TerrainMaterialProto>()
            .Where(x => !x.IgnoreInEditor && x.MinedProduct != null)
            .OrderBy(x => x.MinedProduct.Strings.Name.TranslatedString)
            .ThenBy(x => x.Id.Value)
            .ToArray();

        // SetTileDataNoEvents is intentionally resolved reflectively. It is the game's own
        // full-layer writer, but its visibility has changed between CoI builds. Reflection
        // keeps this mod source compatible while still using the exact installed method.
        m_setTileDataNoEvents = typeof(TerrainManager).GetMethod(
            "SetTileDataNoEvents",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] {
                typeof(int), typeof(HeightTilesF), typeof(TileSurfaceData), typeof(ushort),
                typeof(TerrainMaterialThicknessSlim[]), typeof(int)
            },
            null);

        if (m_setTileDataNoEvents == null) {
            Log.Error("DeepMineMod: TerrainManager.SetTileDataNoEvents was not found; deep painting is unavailable");
        }

        Log.Info($"DeepMineMod: deep resource brush created with {m_materials.Length} mineable resources");
    }

    public void SetMaterial(TerrainMaterialProto material) {
        if (material == null) return;

        for (int i = 0; i < m_materials.Length; i++) {
            if (ReferenceEquals(m_materials[i], material) || m_materials[i].Id.Value == material.Id.Value) {
                m_materialIndex = i;
                logCurrentSettings("resource selected");
                return;
            }
        }

        Log.Warning($"DeepMineMod: ignored non-mineable resource material {material.Id.Value}");
    }

    public void SetRadius(int radius) {
        m_radius = clamp(radius, MinRadius, MaxRadius);
        logCurrentSettings("radius set");
    }

    public void SetThickness(int thickness) {
        m_thickness = clamp(thickness, MinThickness, MaxThickness);
        logCurrentSettings("thickness set");
    }

    public void SetDepth(int depth) {
        m_depth = clamp(depth, MinDepth, MaxDepth);
        logCurrentSettings("depth set");
    }

    public void Activate() {
        if (m_materials.Length == 0) {
            Log.Warning("DeepMineMod: no editor-visible mineable terrain resources are registered");
            return;
        }
        if (m_setTileDataNoEvents == null) {
            Log.Warning("DeepMineMod: deep layer writer is unavailable in this game build");
            return;
        }

        m_isActive = true;
        m_hasLastPaintCenter = false;
        m_terrainCursor.Activate();
        logCurrentSettings("deep painter activated");
    }

    public void Deactivate() {
        if (m_isActive) m_terrainCursor.Deactivate();
        m_isActive = false;
        m_hasLastPaintCenter = false;
        Log.Info("DeepMineMod: deep resource painter deactivated");
    }

    public bool InputUpdate() {
        if (!m_isActive) return false;

        if (Input.GetMouseButtonDown(1)) {
            m_inputManager.DeactivateController(this);
            return true;
        }

        float wheel = Input.mouseScrollDelta.y;
        if (wheel != 0f) {
            int direction = wheel > 0f ? 1 : -1;
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (alt) {
                SetDepth(m_depth + direction);
                return true;
            }
            if (ctrl) {
                SetThickness(m_thickness + direction);
                return true;
            }
            if (shift) {
                SetRadius(m_radius + direction);
                return true;
            }
        }

        if (Input.GetMouseButton(0) && m_terrainCursor.HasValue) {
            Tile2i center = m_terrainCursor.Tile2i;
            if (!m_hasLastPaintCenter || !center.Equals(m_lastPaintCenter)) {
                paintCircle(center);
                m_lastPaintCenter = center;
                m_hasLastPaintCenter = true;
            }
            return true;
        }

        if (Input.GetMouseButtonUp(0)) {
            m_hasLastPaintCenter = false;
        }

        return false;
    }

    private void paintCircle(Tile2i center) {
        TerrainMaterialProto material = m_materials[m_materialIndex];
        int radiusSquared = m_radius * m_radius;
        int changed = 0;
        int failed = 0;

        for (int y = center.Y - m_radius; y <= center.Y + m_radius; y++) {
            for (int x = center.X - m_radius; x <= center.X + m_radius; x++) {
                int dx = x - center.X;
                int dy = y - center.Y;
                if (dx * dx + dy * dy > radiusSquared) continue;

                try {
                    Tile2iAndIndex tile = m_terrainManager.ExtendTileIndex(x, y);
                    Tile2iIndex index = m_terrainManager.GetTileIndex(x, y);

                    if (paintDeepLayer(tile, index, material)) changed++;
                }
                catch (Exception ex) {
                    failed++;
                    if (failed <= 3) {
                        Log.Warning($"DeepMineMod: deep paint skipped tile ({x}, {y}): {ex.Message}");
                    }
                }
            }
        }

        Log.Info(
            $"DeepMineMod: painted deep resource={material.Id.Value}, depth={m_depth}, " +
            $"thickness={m_thickness}, radius={m_radius}, center=({center.X},{center.Y}), " +
            $"changed={changed}, failed={failed}");
    }

    /// <summary>
    /// Rebuilds one tile's layer stack. Existing layers are split into at-most-one-tile
    /// slices only until the bottom of the requested deposit. Slices in the selected depth
    /// band have only their material ID replaced; their exact thickness is preserved.
    /// Everything below the band is appended unchanged.
    /// </summary>
    private bool paintDeepLayer(
        Tile2iAndIndex tile,
        Tile2iIndex index,
        TerrainMaterialProto material)
    {
        var originalLayers = new List<TerrainMaterialThicknessSlim>();
        foreach (TerrainMaterialThicknessSlim layer in m_terrainManager.EnumerateLayers(index)) {
            originalLayers.Add(layer);
        }

        if (originalLayers.Count == 0) return false;

        var oneTile = new ThicknessTilesF(1);
        TerrainMaterialSlimId targetId =
            new TerrainMaterialThicknessSlim(material, oneTile).SlimId;

        int targetStart = m_depth;
        int targetEnd = m_depth + m_thickness;
        int depthCursor = 0;
        int slices = 0;
        bool changed = false;
        bool bandReached = false;

        var rewritten = new List<TerrainMaterialThicknessSlim>(originalLayers.Count + 8);

        for (int layerIndex = 0; layerIndex < originalLayers.Count; layerIndex++) {
            TerrainMaterialThicknessSlim remaining = originalLayers[layerIndex];

            // Once the selected depth band is behind us, preserve every deeper layer exactly.
            if (depthCursor >= targetEnd) {
                rewritten.Add(remaining);
                continue;
            }

            while (!isEmpty(remaining) && depthCursor < targetEnd) {
                if (++slices > MaxSlicesPerTile) {
                    throw new InvalidOperationException(
                        $"terrain layer stack exceeded {MaxSlicesPerTile} slices before depth {targetEnd}");
                }

                TerrainMaterialThicknessSlim piece = remaining.RemoveAsMuchAs(oneTile, out TerrainMaterialThicknessSlim next);
                if (isEmpty(piece)) break;

                bool insideBand = depthCursor >= targetStart && depthCursor < targetEnd;
                if (insideBand) {
                    bandReached = true;
                    if (!piece.SlimId.Equals(targetId)) changed = true;
                    piece = piece.WithNewId(targetId);
                }

                rewritten.Add(piece);
                depthCursor++;
                remaining = next;
            }

            // We stopped because the deposit ended inside this original layer. Preserve
            // the untouched remainder of that layer as one layer instead of slicing deeper.
            if (!isEmpty(remaining)) {
                rewritten.Add(remaining);
            }
        }

        if (!bandReached || !changed) return false;

        HeightTilesF height = m_terrainManager.GetHeight(index);
        TileSurfaceData surface = m_terrainManager.GetTileSurface(index);
        ushort flags = (ushort)m_terrainManager.GetTileFlags(index);
        int rawIndex = getRawTileIndex(index);
        TerrainMaterialThicknessSlim[] layers = rewritten.ToArray();

        m_setTileDataNoEvents.Invoke(
            m_terrainManager,
            new object[] { rawIndex, height, surface, flags, layers, layers.Length });

        // Total thickness did not change, so this is a material-only notification. This
        // updates rendering/mining/resource consumers without moving the terrain surface.
        m_terrainManager.NotifyTileMaterialsOnlyChanged(tile);
        return true;
    }

    private static bool isEmpty(TerrainMaterialThicknessSlim layer) {
        return layer.Equals(default(TerrainMaterialThicknessSlim));
    }

    /// <summary>
    /// Tile2iIndex is a compact value type. Its backing member name has changed between
    /// game builds, so obtain the single integer value reflectively instead of hardcoding it.
    /// </summary>
    private static int getRawTileIndex(Tile2iIndex index) {
        Type type = typeof(Tile2iIndex);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object boxed = index;

        PropertyInfo property = type.GetProperties(flags)
            .FirstOrDefault(p => p.PropertyType == typeof(int) && p.GetIndexParameters().Length == 0);
        if (property != null) return (int)property.GetValue(boxed, null);

        FieldInfo field = type.GetFields(flags)
            .FirstOrDefault(f => f.FieldType == typeof(int));
        if (field != null) return (int)field.GetValue(boxed);

        throw new InvalidOperationException("Unable to read Tile2iIndex raw integer value");
    }

    private void logCurrentSettings(string reason) {
        if (m_materials.Length == 0) return;
        TerrainMaterialProto material = m_materials[m_materialIndex];
        string product = material.MinedProduct == null
            ? material.Id.Value
            : material.MinedProduct.Strings.Name.TranslatedString;

        Log.Info(
            $"DeepMineMod: {reason}; resource={product} ({material.Id.Value}), " +
            $"depth={m_depth}, thickness={m_thickness}, radius={m_radius}");
    }

    private static int clamp(int value, int minimum, int maximum) {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
}
