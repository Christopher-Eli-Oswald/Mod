using Mafi;
using UnityEngine;

namespace DeepMineMod;

/// <summary>
/// Draws the current deep-resource brush footprint on the terrain surface.
/// This is only a placement preview; the resource itself is written underground.
/// </summary>
public sealed class DeepMinePreviewCircleMb : MonoBehaviour {
    private const int Segments = 48;
    private const float MafiTileToUnity = 2f;

    private LineRenderer m_lineRenderer;
    private Tile3f m_center;
    private int m_radius = 1;
    private bool m_dirty = true;

    private void Awake() {
        m_lineRenderer = gameObject.AddComponent<LineRenderer>();
        m_lineRenderer.useWorldSpace = true;
        m_lineRenderer.loop = true;
        m_lineRenderer.positionCount = Segments;
        m_lineRenderer.startWidth = 0.4f;
        m_lineRenderer.endWidth = 0.4f;
        m_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        SetColor(new Color(0.2f, 1f, 0.35f, 0.95f));
    }

    public void SetCenter(Tile3f center) {
        m_center = center;
        m_dirty = true;
    }

    public void SetRadius(int radiusTiles) {
        m_radius = radiusTiles;
        m_dirty = true;
    }

    public void SetColor(Color color) {
        if (m_lineRenderer == null) return;
        m_lineRenderer.startColor = color;
        m_lineRenderer.endColor = color;
    }

    public void SetVisible(bool visible) {
        if (m_lineRenderer != null) m_lineRenderer.enabled = visible;
    }

    private void LateUpdate() {
        if (!m_dirty || m_lineRenderer == null) return;
        m_dirty = false;

        Vector3 center = m_center.ToVector3();
        float radius = m_radius * MafiTileToUnity;

        for (int i = 0; i < Segments; i++) {
            float angle = (i / (float)Segments) * 2f * Mathf.PI;
            m_lineRenderer.SetPosition(
                i,
                new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + 0.5f,
                    center.z + Mathf.Sin(angle) * radius));
        }
    }
}
