using UnityEngine;

public class ScoreOnGUI : MonoBehaviour
{
    public bool bottomLeft = true;
    public Vector2 margin = new Vector2(16, 16);
    public int fontSize = 22;

    GUIStyle _style;
    Texture2D _bg;

    void EnsureInit()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft
            };
            _style.normal.textColor = Color.white;
        }
        if (_bg == null) _bg = Texture2D.whiteTexture;
    }

    void OnGUI()
    {
        EnsureInit();
        if (ScoreManager.I == null) return;

        float w = 240f, h = 36f;
        float x = margin.x;
        float y = bottomLeft ? (Screen.height - h - margin.y) : margin.y;

        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(x - 8, y - 4, w + 16, h + 8), _bg);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, h), $"Puntos: {ScoreManager.I.score:0}", _style);
    }
}
