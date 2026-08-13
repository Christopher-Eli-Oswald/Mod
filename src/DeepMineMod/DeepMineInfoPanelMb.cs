using Mafi.Unity.UiStatic;
using UnityEngine;

namespace DeepMineMod;

/// <summary>
/// Small floating panel beside the cursor that shows the active deep-resource settings.
/// </summary>
public sealed class DeepMineInfoPanelMb : MonoBehaviour {
    private const float PanelWidth = 240f;
    private const float CursorOffset = 18f;
    private const float VerticalPadding = 8f;
    private const float HorizontalPadding = 8f;

    private string[] m_lines = System.Array.Empty<string>();
    private GUIStyle m_style;
    private GUIStyle m_backgroundStyle;
    private float m_lineHeight = -1f;

    public void SetLines(params string[] lines) {
        m_lines = lines ?? System.Array.Empty<string>();
    }

    private void OnGUI() {
        if (m_lines.Length == 0) return;

        if (m_style == null) {
            m_style = new GUIStyle(GUI.skin.label) {
                fontSize = 14,
                normal = { textColor = Color.white },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
            m_backgroundStyle = new GUIStyle(GUI.skin.box);
            m_lineHeight = m_style.CalcSize(new GUIContent("Mg")).y;
        }

        float scale = UiScaleHelper.GetCurrentScaleFloat();
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        Vector2 mouse = new Vector2(
            Input.mousePosition.x,
            Screen.height - Input.mousePosition.y).ApplyScale();

        float height = m_lines.Length * m_lineHeight + VerticalPadding * 2f;
        Rect panel = new Rect(
            mouse.x + CursorOffset,
            mouse.y + CursorOffset,
            PanelWidth,
            height);

        GUI.Box(panel, GUIContent.none, m_backgroundStyle);

        for (int i = 0; i < m_lines.Length; i++) {
            Rect line = new Rect(
                panel.x + HorizontalPadding,
                panel.y + VerticalPadding + i * m_lineHeight,
                panel.width - HorizontalPadding * 2f,
                m_lineHeight);
            GUI.Label(line, m_lines[i], m_style);
        }
    }
}
