using UnityEngine;

// Ekranda elde görünen silah + kollar. Ayrı bir kamerada çizilir, böylece duvara girmez.
public class OgeViewModel : MonoBehaviour
{
    public static OgeViewModel I;

    Transform root, mag, muzzle;
    GameObject flashG;
    Vector3 basePos, magBase;
    float kz, kzv, kr, krv, flashT, bobT, bobAmt;
    Vector2 sway;
    int cur = -1;

    void Awake()
    {
        I = this;
        root = new GameObject("VMRoot").transform;
        root.SetParent(transform, false);
        root.gameObject.layer = OgeArt.VMLayer;
        Show(false);
    }

    public void Show(bool on) { if (root) root.gameObject.SetActive(on); if (!on) cur = -1; }

    public Vector3 MuzzleWorld => muzzle ? muzzle.position : transform.position;

    GameObject P(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default, float smooth = 0.35f)
    {
        return OgeArt.Prim(t, parent, pos, scale, OgeArt.I.LitC(c, smooth), euler, false, OgeArt.VMLayer, false);
    }

    public void SetWeapon(int w, Color sleeve)
    {
        if (w == cur) return;
        cur = w;
        for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        mag = null; muzzle = null;

        Color dark = new Color(0.2f, 0.22f, 0.26f), dark2 = new Color(0.13f, 0.14f, 0.17f), wood = new Color(0.56f, 0.36f, 0.2f);
        Color skin = new Color(0.96f, 0.78f, 0.62f), metal = new Color(0.8f, 0.82f, 0.86f), green = new Color(0.2f, 0.38f, 0.25f);
        var g = new GameObject("silah").transform;
        g.SetParent(root, false);
        g.gameObject.layer = OgeArt.VMLayer;
        Vector3 mz;

        switch (w)
        {
            case 0: // tüfek
                P(PrimitiveType.Cube, g, new Vector3(0, 0, 0.06f), new Vector3(0.06f, 0.085f, 0.36f), dark);
                P(PrimitiveType.Cube, g, new Vector3(0, -0.005f, 0.31f), new Vector3(0.066f, 0.072f, 0.2f), wood, default, 0.2f);
                P(PrimitiveType.Cylinder, g, new Vector3(0, 0.015f, 0.47f), new Vector3(0.024f, 0.09f, 0.024f), dark2, new Vector3(90, 0, 0), 0.6f);
                mag = P(PrimitiveType.Cube, g, new Vector3(0, -0.1f, 0.12f), new Vector3(0.045f, 0.14f, 0.07f), dark2, new Vector3(14, 0, 0)).transform;
                P(PrimitiveType.Cube, g, new Vector3(0, -0.025f, -0.2f), new Vector3(0.05f, 0.095f, 0.17f), wood, default, 0.2f);
                P(PrimitiveType.Cube, g, new Vector3(0, -0.08f, -0.04f), new Vector3(0.04f, 0.1f, 0.05f), dark, new Vector3(-15, 0, 0));
                P(PrimitiveType.Cube, g, new Vector3(0, 0.06f, 0.0f), new Vector3(0.018f, 0.035f, 0.06f), dark2);
                mz = new Vector3(0, 0.015f, 0.57f);
                Arm(g, new Vector3(0.0f, -0.075f, -0.05f), new Vector3(0.07f, -0.2f, -0.24f), sleeve, skin);
                Arm(g, new Vector3(0.0f, -0.05f, 0.3f), new Vector3(-0.16f, -0.2f, 0.12f), sleeve, skin);
                basePos = new Vector3(0.17f, -0.16f, 0.36f);
                break;
            case 1: // tabanca
                P(PrimitiveType.Cube, g, new Vector3(0, 0.03f, 0.06f), new Vector3(0.042f, 0.046f, 0.19f), new Color(0.42f, 0.44f, 0.48f), default, 0.6f);
                P(PrimitiveType.Cube, g, new Vector3(0, 0.0f, 0.05f), new Vector3(0.038f, 0.025f, 0.17f), dark);
                P(PrimitiveType.Cube, g, new Vector3(0, -0.06f, -0.02f), new Vector3(0.036f, 0.11f, 0.05f), dark2, new Vector3(-12, 0, 0));
                mag = P(PrimitiveType.Cube, g, new Vector3(0, -0.1f, -0.03f), new Vector3(0.03f, 0.04f, 0.04f), dark2).transform;
                mz = new Vector3(0, 0.03f, 0.16f);
                Arm(g, new Vector3(0.0f, -0.07f, -0.04f), new Vector3(0.08f, -0.2f, -0.22f), sleeve, skin);
                Arm(g, new Vector3(-0.025f, -0.075f, -0.02f), new Vector3(-0.14f, -0.22f, -0.16f), sleeve, skin);
                basePos = new Vector3(0.13f, -0.14f, 0.32f);
                break;
            case 2: // keskin nişancı
                P(PrimitiveType.Cube, g, new Vector3(0, 0, 0.1f), new Vector3(0.06f, 0.08f, 0.5f), green, default, 0.25f);
                P(PrimitiveType.Cylinder, g, new Vector3(0, 0.015f, 0.5f), new Vector3(0.02f, 0.18f, 0.02f), dark2, new Vector3(90, 0, 0), 0.6f);
                P(PrimitiveType.Cylinder, g, new Vector3(0, 0.08f, 0.08f), new Vector3(0.036f, 0.11f, 0.036f), dark2, new Vector3(90, 0, 0), 0.7f);
                P(PrimitiveType.Cylinder, g, new Vector3(0, 0.08f, 0.195f), new Vector3(0.03f, 0.004f, 0.03f), new Color(0.4f, 0.7f, 0.9f), new Vector3(90, 0, 0), 0.95f);
                mag = P(PrimitiveType.Cube, g, new Vector3(0, -0.075f, 0.06f), new Vector3(0.04f, 0.07f, 0.06f), dark2).transform;
                P(PrimitiveType.Cube, g, new Vector3(0, -0.03f, -0.24f), new Vector3(0.05f, 0.11f, 0.2f), green, default, 0.25f);
                P(PrimitiveType.Cube, g, new Vector3(0, -0.08f, -0.06f), new Vector3(0.04f, 0.1f, 0.05f), dark, new Vector3(-15, 0, 0));
                mz = new Vector3(0, 0.015f, 0.69f);
                Arm(g, new Vector3(0.0f, -0.075f, -0.07f), new Vector3(0.07f, -0.2f, -0.26f), sleeve, skin);
                Arm(g, new Vector3(0.0f, -0.05f, 0.26f), new Vector3(-0.16f, -0.2f, 0.1f), sleeve, skin);
                basePos = new Vector3(0.17f, -0.16f, 0.34f);
                break;
            default: // karambit
                P(PrimitiveType.Cube, g, new Vector3(0, 0, 0), new Vector3(0.03f, 0.032f, 0.11f), dark2);
                P(PrimitiveType.Cube, g, new Vector3(0, 0.03f, 0.1f), new Vector3(0.008f, 0.026f, 0.13f), metal, new Vector3(-28, 0, 0), 0.85f);
                P(PrimitiveType.Cube, g, new Vector3(0, 0.07f, 0.155f), new Vector3(0.008f, 0.022f, 0.06f), metal, new Vector3(-75, 0, 0), 0.85f);
                mz = new Vector3(0, 0.05f, 0.15f);
                Arm(g, new Vector3(0, -0.01f, -0.02f), new Vector3(0.07f, -0.17f, -0.22f), sleeve, skin);
                basePos = new Vector3(0.16f, -0.17f, 0.3f);
                break;
        }

        muzzle = new GameObject("namlu").transform;
        muzzle.SetParent(g, false);
        muzzle.localPosition = mz;
        flashG = P(PrimitiveType.Sphere, muzzle, Vector3.zero, Vector3.one * 0.07f, Color.white);
        flashG.GetComponent<Renderer>().sharedMaterial = OgeArt.I.UnlitC(new Color(1f, 0.85f, 0.45f));
        flashG.SetActive(false);
        if (mag) magBase = mag.localPosition;
        kz = kzv = kr = krv = 0;
    }

    // El + ön kol: el silahta, kol ekranın dışına doğru uzanır.
    void Arm(Transform g, Vector3 hand, Vector3 elbow, Color sleeve, Color skin)
    {
        P(PrimitiveType.Cube, g, hand, new Vector3(0.065f, 0.075f, 0.09f), skin, default, 0.2f);
        Vector3 mid = (hand + elbow) * 0.5f, dir = elbow - hand;
        var arm = P(PrimitiveType.Capsule, g, mid, new Vector3(0.085f, dir.magnitude * 0.55f, 0.085f), sleeve, default, 0.15f);
        arm.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        var cuff = P(PrimitiveType.Cylinder, g, hand + dir.normalized * 0.06f, new Vector3(0.09f, 0.015f, 0.09f), Color.white, default, 0.1f);
        cuff.transform.localRotation = arm.transform.localRotation;
    }

    public void Kick(float k)
    {
        kzv += k * 1.6f / 0.018f * 0.5f;
        krv += k * 2200f;
    }

    public void Flash() { flashT = 0.045f; }

    static float Ease(float x) => x * x * (3 - 2 * x);

    // draw/rel/sw/ins: 0..1 ilerleme, kullanılmıyorsa -1
    public void Tick(float dt, bool moving, Vector2 look, float draw, float rel, float sw, bool heavy, float ins, bool hide, bool knife)
    {
        if (!root || cur < 0) return;
        root.gameObject.SetActive(!hide);

        krv += (-90f * kr - 13f * krv) * dt; kr += krv * dt;
        kzv += (-150f * kz - 17f * kzv) * dt; kz += kzv * dt;
        var swT = new Vector2(Mathf.Clamp(-look.x * 0.0005f, -0.03f, 0.03f), Mathf.Clamp(-look.y * 0.0005f, -0.03f, 0.03f));
        sway = Vector2.Lerp(sway, swT, 1 - Mathf.Exp(-9f * dt));
        bobAmt = Mathf.MoveTowards(bobAmt, moving ? 1 : 0, dt * 4f);
        bobT += dt * 9.5f * bobAmt;

        Vector3 p = basePos + new Vector3(sway.x + Mathf.Sin(bobT) * 0.011f * bobAmt, sway.y - Mathf.Abs(Mathf.Cos(bobT)) * 0.009f * bobAmt, -kz);
        Vector3 e = new Vector3(-kr, sway.x * 120f, 0);

        if (draw > 0) { float d = Ease(draw); p.y -= d * 0.32f; e.x += d * 60f; }
        if (rel >= 0)
        {
            float s = Mathf.Sin(rel * Mathf.PI);
            p.y -= 0.06f * s; p.x -= 0.03f * s; e.z += 32f * s; e.x -= 14f * s;
            if (mag)
            {
                float m = rel < 0.2f ? 0 : rel < 0.4f ? Mathf.InverseLerp(0.2f, 0.4f, rel) : rel < 0.6f ? 1 : rel < 0.8f ? 1 - Mathf.InverseLerp(0.6f, 0.8f, rel) : 0;
                mag.localPosition = magBase + Vector3.down * 0.25f * m;
            }
        }
        else if (mag) mag.localPosition = magBase;

        if (sw >= 0)
        {
            float s = Mathf.Sin(sw * Mathf.PI);
            if (heavy) { p.z += 0.16f * s; p.x -= 0.05f * s; e.x -= 12f * s; }
            else { float a = Ease(sw); e.z += Mathf.Lerp(45f, -80f, a) * s; e.y += Mathf.Lerp(-20f, 30f, a) * s; p.x += Mathf.Lerp(0.06f, -0.14f, a) * s; e.x -= 18f * s; }
        }
        if (ins >= 0)
        {
            float tw = Ease(Mathf.Clamp01(ins / 0.2f)) * Ease(Mathf.Clamp01((1 - ins) / 0.2f));
            if (knife) { e.x -= 360f * Ease(Mathf.Clamp01((ins - 0.15f) / 0.5f)); p.y += 0.04f * tw; p.x -= 0.04f * tw; e.z += 20f * tw; }
            else { e.y -= 55f * tw; e.z += 28f * tw; p.x -= 0.06f * tw; p.y += 0.03f * tw; }
        }

        root.localPosition = p;
        root.localRotation = Quaternion.Euler(e);
        flashT -= dt;
        if (flashG) { flashG.SetActive(flashT > 0); flashG.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360)); }
    }
}
