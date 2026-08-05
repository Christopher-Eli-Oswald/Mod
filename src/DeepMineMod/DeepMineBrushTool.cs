using System;
using System.Linq;
using Mafi;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain;
using Mafi.Unity.InputControl;
using UnityEngine;

namespace DeepMineMod;

/// <summary>
/// Experimental underground-resource brush for Captain of Industry 0.8.6c.
///
/// F10 activates the tool.
/// Alt + mouse wheel changes the terrain material.
/// Shift + mouse wheel changes the circular brush radius.
/// Ctrl + mouse wheel changes deposit thickness.
/// Left click paints the deposit without changing surface height.
/// Right click exits.
///
/// The tool deliberately calls TerrainManager's public
/// TryAddMaterialToUndergroundTopFourLayer_NoHeightChange API instead of
/// modifying raw terrain arrays. This keeps terrain notifications and save
/// tracking inside the game-owned terrain manager.
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
    private readonly KeyBindings m_binding =
        KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10);

    private int m_materialIndex;
    private int m_radius = 8;
    private int m_thickness = 10;
    private bool m_isActive;

    public ControllerConfig Config => ControllerConfig.ToolBlockingCamera;

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
            .OrderBy(x => x.Id.Value)
            .ToArray();

        m_inputManager.RegisterGlobalShortcut(_ => m_binding, this);

        Log.Info($"DeepMineMod: F10 brush registered with {m_materials.Length} terrain materials");
    }

    public void Activate() {
        if (m_materials.Length == 0) {
            Log.Warning("DeepMineMod: no terrain materials were registered; brush cannot activate");
            return;
        }

        m_isActive = true;
        m_terrainCursor.Activate();
        logCurrentSettings("activated");
    }

    public void Deactivate() {
        if (m_isActive) {
            m_terrainCursor.Deactivate();
        }
        m_isActive = false;
        Log.Info("DeepMineMod: brush deactivated");
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
                cycleMaterial(direction);
                logCurrentSettings("material changed");
                return true;
            }
            if (ctrl) {
                m_thickness = clamp(m_thickness + direction, MinThickness, MaxThickness);
                logCurrentSettings("thickness changed");
                return true;
            }
            if (shift) {
                m_radius = clamp(m_radius + direction, MinRadius, MaxRadius);
                logCurrentSettings("radius changed");
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0) && m_terrainCursor.HasValue) {
            paintCircle(m_terrainCursor.Tile2i);
            return true;
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
        int rejected = 0;

        for (int y = center.Y - m_radius; y <= center.Y + m_radius; y++) {
            for (int x = center.X - m_radius; x <= center.X + m_radius; x++) {
                int dx = x - center.X;
                int dy = y - center.Y;
                if (dx * dx + dy * dy > radiusSquared) continue;

                try {
                    Tile2iAndIndex tile = m_terrainManager.ExtendTileIndex(x, y);
                    if (m_terrainManager.TryAddMaterialToUndergroundTopFourLayer_NoHeightChange(tile, layer)) {
                        changed++;
                    } else {
                        rejected++;
                    }
                } catch (Exception ex) {
                    rejected++;
                    Log.Warning($"DeepMineMod: skipped tile ({x}, {y}): {ex.Message}");
                }
            }
        }

        Log.Info(
            $"DeepMineMod: painted {material.Id.Value}, thickness {m_thickness}, " +
            $"radius {m_radius} at ({center.X}, {center.Y}); changed={changed}, rejected={rejected}");
    }

    private void cycleMaterial(int direction) {
        int count = m_materials.Length;
        m_materialIndex = ((m_materialIndex + direction) % count + count) % count;
    }

    private void logCurrentSettings(string reason) {
        TerrainMaterialProto material = m_materials[m_materialIndex];
        Log.Info(
            $"DeepMineMod: brush {reason}; material={material.Id.Value}, " +
            $"radius={m_radius}, thickness={m_thickness}");
    }

    private static int clamp(int value, int minimum, int maximum) {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
}
