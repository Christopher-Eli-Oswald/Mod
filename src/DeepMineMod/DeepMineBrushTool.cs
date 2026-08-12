using System;
using System.Linq;
using Mafi;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using UnityEngine;

namespace DeepMineMod;

/// <summary>
/// Live-save resource painter modeled after the map editor's mineable-resource workflow.
///
/// The real map editor stores terrain features and then regenerates terrain. Re-running that
/// generator over an established save would be destructive, so this controller mirrors the
/// editor's resource/material selection and brush behavior while applying the resource layer
/// directly to the existing TerrainManager.
///
/// Left mouse paints continuously while dragging. Right mouse exits.
/// Shift + wheel changes radius. Ctrl + wheel changes deposit thickness.
/// </summary>
[GlobalDependency(RegistrationMode.AsEverything, false, false)]
public sealed class DeepMineBrushTool : IUnityInputController {
    private const int MinRadius = 1;
    private const int MaxRadius = 100;
    private const int MinThickness = 1;
    private const int MaxThickness = 100;

    private readonly IUnityInputMgr m_inputManager;
    private readonly TerrainCursor m_terrainCursor;
    private readonly TerrainManager m_terrainManager;
    private readonly TerrainMaterialProto[] m_materials;

    private int m_materialIndex;
    private int m_radius = 8;
    private int m_thickness = 10;
    private bool m_isActive;
    private bool m_hasLastPaintCenter;
    private Tile2i m_lastPaintCenter;

    public ControllerConfig Config => ControllerConfig.ToolBlockingCamera;

    /// <summary>
    /// Same practical catalog the map editor exposes for mineable terrain resources:
    /// editor-visible TerrainMaterialProto entries which produce a mined product.
    /// </summary>
    public TerrainMaterialProto[] Materials => m_materials;

    public TerrainMaterialProto SelectedMaterial =>
        m_materials.Length == 0 ? null : m_materials[m_materialIndex];

    public int Radius => m_radius;
    public int Thickness => m_thickness;

    public DeepMineBrushTool(
        IUnityInputMgr inputManager,
        TerrainCursor terrainCursor,
        TerrainManager terrainManager,
        ProtosDb protosDb)
    {
        m_inputManager = inputManager;
        m_terrainCursor = terrainCursor;
        m_terrainManager = terrainManager;

        // Map-editor style resource list: don't expose decorative/internal terrain materials.
        // A terrain material is a resource when it is editor-visible and has a mined product.
        m_materials = protosDb.All<TerrainMaterialProto>()
            .Where(x => !x.IgnoreInEditor && x.MinedProduct != null)
            .OrderBy(x => x.MinedProduct.Strings.Name.TranslatedString)
            .ThenBy(x => x.Id.Value)
            .ToArray();

        Log.Info($"DeepMineMod: live map-editor brush created with {m_materials.Length} mineable resources");
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

        Log.Warning($"DeepMineMod: ignored non-map-editor resource material {material.Id.Value}");
    }

    public void SetRadius(int radius) {
        m_radius = clamp(radius, MinRadius, MaxRadius);
        logCurrentSettings("radius set");
    }

    public void SetThickness(int thickness) {
        m_thickness = clamp(thickness, MinThickness, MaxThickness);
        logCurrentSettings("thickness set");
    }

    public void Activate() {
        if (m_materials.Length == 0) {
            Log.Warning("DeepMineMod: no editor-visible mineable terrain resources are registered");
            return;
        }

        m_isActive = true;
        m_hasLastPaintCenter = false;
        m_terrainCursor.Activate();
        logCurrentSettings("live editor activated");
    }

    public void Deactivate() {
        if (m_isActive) m_terrainCursor.Deactivate();
        m_isActive = false;
        m_hasLastPaintCenter = false;
        Log.Info("DeepMineMod: live map-editor resource painter deactivated");
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
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ctrl) {
                SetThickness(m_thickness + direction);
                return true;
            }
            if (shift) {
                SetRadius(m_radius + direction);
                return true;
            }
        }

        // Map-editor style painting: holding LMB paints continuously as the cursor moves.
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
        var layer = new TerrainMaterialThicknessSlim(
            material,
            new ThicknessTilesF(m_thickness));

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

                    // Preserve the developed surface. The map editor normally achieves its final
                    // resource layers during terrain generation; in a live save we write that
                    // mineable layer below the current surface instead of regenerating the map.
                    m_terrainManager.DumpMaterialToSecondLayer_NoHeightChange(tile, layer);
                    changed++;
                }
                catch (Exception ex) {
                    failed++;
                    if (failed <= 3) {
                        Log.Warning($"DeepMineMod: resource paint skipped tile ({x}, {y}): {ex.Message}");
                    }
                }
            }
        }

        Log.Info(
            $"DeepMineMod: live editor painted resource={material.Id.Value}, " +
            $"thickness={m_thickness}, radius={m_radius}, center=({center.X},{center.Y}), " +
            $"changed={changed}, failed={failed}");
    }

    private void logCurrentSettings(string reason) {
        if (m_materials.Length == 0) return;
        TerrainMaterialProto material = m_materials[m_materialIndex];
        string product = material.MinedProduct == null
            ? material.Id.Value
            : material.MinedProduct.Strings.Name.TranslatedString;

        Log.Info(
            $"DeepMineMod: {reason}; resource={product} ({material.Id.Value}), " +
            $"radius={m_radius}, thickness={m_thickness}");
    }

    private static int clamp(int value, int minimum, int maximum) {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
}
