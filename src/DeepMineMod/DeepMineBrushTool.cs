using System;
using System.Linq;
using Mafi;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using UnityEngine;

namespace DeepMineMod;

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

    public ControllerConfig Config => ControllerConfig.ToolBlockingCamera;
    public TerrainMaterialProto[] Materials => m_materials;
    public TerrainMaterialProto SelectedMaterial =>
        m_materials.Length == 0 ? null : m_materials[m_materialIndex];

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

        Log.Info($"DeepMineMod: brush created with {m_materials.Length} terrain materials");
    }

    public void SetMaterial(TerrainMaterialProto material) {
        if (material == null) return;

        for (int i = 0; i < m_materials.Length; i++) {
            if (ReferenceEquals(m_materials[i], material) || m_materials[i].Id.Value == material.Id.Value) {
                m_materialIndex = i;
                logCurrentSettings("material selected");
                return;
            }
        }

        Log.Warning($"DeepMineMod: ignored unknown terrain material {material.Id.Value}");
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
        if (count == 0) return;
        m_materialIndex = ((m_materialIndex + direction) % count + count) % count;
    }

    private void logCurrentSettings(string reason) {
        if (m_materials.Length == 0) return;
        TerrainMaterialProto material = m_materials[m_materialIndex];
        Log.Info(
            $"DeepMineMod: brush {reason}; material={material.Id.Value}, " +
            $"radius={m_radius}, thickness={m_thickness}");
    }

    private static int clamp(int value, int minimum, int maximum) {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
}
