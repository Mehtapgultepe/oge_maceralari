using UnityEngine;

// Ekran yazıları için ortak stiller (720 piksel yüksekliğe göre ölçeklenir).
public static class OgeUI
{
    public static GUIStyle Panel, Title, Sub, Text, Small, Field, CodeField, Btn, Btn2, Big, Mid, Pill, Banner;
    public static Texture2D White;
    static bool ready;
    static readonly Color Ink = new Color(0.15f, 0.13f, 0.36f);
    static readonly Color Accent = new Color(0.5f, 0.47f, 0.87f);

    public static Texture2D Round(Color c, int r = 14)
    {
        int s = r * 2 + 4;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x, x - (s - 1 - r)));
                float dy = Mathf.Max(0, Mathf.Max(r - y, y - (s - 1 - r)));
                float a = Mathf.Clamp01(r + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        t.Apply();
        return t;
    }

    static GUIStyle Box(Color c, int r, int pad)
    {
        var s = new GUIStyle();
        s.normal.background = Round(c, r);
        s.border = new RectOffset(r + 1, r + 1, r + 1, r + 1);
        s.padding = new RectOffset(pad, pad, pad, pad);
        return s;
    }

    static GUIStyle Lbl(int size, Color col, FontStyle fs = FontStyle.Normal, TextAnchor a = TextAnchor.MiddleCenter)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fs, alignment = a, wordWrap = true };
        s.normal.textColor = col;
        return s;
    }

    static void Init()
    {
        if (ready) return;
        ready = true;
        White = Texture2D.whiteTexture;
        Panel = Box(new Color(1f, 1f, 1f, 0.9f), 18, 16);
        Pill = Box(new Color(0.15f, 0.13f, 0.36f, 0.72f), 14, 6);
        Banner = Box(new Color(0.15f, 0.13f, 0.36f, 0.8f), 16, 10);
        Title = Lbl(40, Ink, FontStyle.Bold);
        Sub = Lbl(18, Accent, FontStyle.Bold);
        Text = Lbl(17, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
        Small = Lbl(14, new Color(0.35f, 0.33f, 0.5f));
        Big = Lbl(40, Color.white, FontStyle.Bold);
        Mid = Lbl(20, Color.white, FontStyle.Bold);

        Field = new GUIStyle(GUI.skin.textField) { fontSize = 18, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 4, 4) };
        Field.normal.background = Field.focused.background = Field.hover.background = Round(new Color(0.94f, 0.93f, 0.99f), 10);
        Field.border = new RectOffset(11, 11, 11, 11);
        Field.normal.textColor = Field.focused.textColor = Field.hover.textColor = Ink;
        CodeField = new GUIStyle(Field) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        Btn = Box(Accent, 14, 8);
        Btn.hover.background = Round(new Color(0.42f, 0.39f, 0.8f), 14);
        Btn.active.background = Round(new Color(0.33f, 0.3f, 0.7f), 14);
        Btn.fontSize = 20; Btn.fontStyle = FontStyle.Bold; Btn.alignment = TextAnchor.MiddleCenter;
        Btn.normal.textColor = Btn.hover.textColor = Btn.active.textColor = Color.white;
        Btn2 = new GUIStyle(Btn);
        Btn2.normal.background = Round(new Color(0.3f, 0.7f, 0.5f), 14);
        Btn2.hover.background = Round(new Color(0.25f, 0.62f, 0.44f), 14);
    }

    public static void Begin(out float W, out float H)
    {
        Init();
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        W = Screen.width / s; H = 720f;
    }

    public static void Shadow(Rect r, string t, GUIStyle st)
    {
        var c = st.normal.textColor;
        st.normal.textColor = new Color(0, 0, 0, 0.45f);
        GUI.Label(new Rect(r.x + 1.5f, r.y + 2f, r.width, r.height), t, st);
        st.normal.textColor = c;
        GUI.Label(r, t, st);
    }

    public static void Fill(Rect r, Color c)
    {
        var o = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, White);
        GUI.color = o;
    }
}
