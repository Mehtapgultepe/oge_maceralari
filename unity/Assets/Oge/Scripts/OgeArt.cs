using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sahnedeki ortak malzemeler, kameralar ve küçük efekt yardımcıları.
public class OgeArt : MonoBehaviour
{
    public static OgeArt I;
    public Material lit, unlit;
    public Camera mainCam, vmCam;

    public static readonly Color Red = new Color(0.886f, 0.294f, 0.29f);
    public static readonly Color Blue = new Color(0.216f, 0.541f, 0.867f);
    public const int VMLayer = 31;

    static readonly Vector3[] SP = { new Vector3(0, 0.1f, 33f), new Vector3(0, 0.1f, -33f) };
    public static Vector3 Spawn(int team) => SP[team];
    public static float SpawnYaw(int team) => team == 0 ? 180f : 0f;

    readonly Dictionary<int, Material> litCache = new Dictionary<int, Material>();
    readonly Dictionary<int, Material> unlitCache = new Dictionary<int, Material>();

    void Awake()
    {
        I = this;
        OgeAudio.Init();
        Application.targetFrameRate = 120;
    }

    static int Key(Color c, float s) =>
        Mathf.RoundToInt(c.r * 255) | Mathf.RoundToInt(c.g * 255) << 8 | Mathf.RoundToInt(c.b * 255) << 16 | Mathf.RoundToInt(s * 63) << 24;

    public Material LitC(Color c, float smooth = 0.3f)
    {
        int k = Key(c, smooth);
        if (!litCache.TryGetValue(k, out var m))
        {
            m = new Material(lit) { color = c };
            m.SetFloat("_Smoothness", smooth);
            litCache[k] = m;
        }
        return m;
    }

    public Material UnlitC(Color c)
    {
        int k = Key(c, 0);
        if (!unlitCache.TryGetValue(k, out var m)) { m = new Material(unlit) { color = c }; unlitCache[k] = m; }
        return m;
    }

    // Çalışma anında basit parça üretir (çarpıştırıcısız, istenirse tutulur).
    public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 lp, Vector3 ls, Material m, Vector3 le = default, bool keepCollider = false, int layer = 0, bool shadows = true)
    {
        var g = GameObject.CreatePrimitive(t);
        g.layer = layer;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        g.transform.localScale = ls;
        g.transform.localEulerAngles = le;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        if (!shadows) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
        if (!keepCollider) Destroy(g.GetComponent<Collider>());
        return g;
    }

    // ---- efektler ----
    public void Tracer(Vector3 a, Vector3 b)
    {
        var g = new GameObject("iz");
        var lr = g.AddComponent<LineRenderer>();
        lr.sharedMaterial = UnlitC(new Color(1f, 0.92f, 0.6f));
        lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
        lr.startWidth = 0.025f; lr.endWidth = 0.01f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(g, 0.05f);
    }

    public void Impact(Vector3 p, Vector3 n)
    {
        var d = Prim(PrimitiveType.Sphere, null, p + n * 0.01f, Vector3.one * 0.07f, LitC(new Color(0.2f, 0.18f, 0.15f), 0f), default, false, 0, false);
        Destroy(d, 8f);
        StartCoroutine(Puff(p + n * 0.05f, new Color(0.85f, 0.8f, 0.7f), 0.35f, 0.35f));
    }

    public IEnumerator Puff(Vector3 p, Color c, float size, float dur)
    {
        var g = Prim(PrimitiveType.Sphere, null, p, Vector3.one * 0.05f, LitC(c, 0f), default, false, 0, false);
        float t = 0;
        while (t < dur && g)
        {
            t += Time.deltaTime;
            g.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, size, t / dur);
            g.transform.position += Vector3.up * Time.deltaTime * 0.4f;
            yield return null;
        }
        if (g) Destroy(g);
    }

    public void MuzzleLight(Vector3 p)
    {
        var g = new GameObject("flaş");
        g.transform.position = p;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.range = 6f; l.intensity = 3f; l.color = new Color(1f, 0.8f, 0.45f);
        Destroy(g, 0.05f);
    }
}

public class OgeHit : MonoBehaviour
{
    public OgePlayer owner;
    public bool head;
}
