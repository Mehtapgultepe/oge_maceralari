using UnityEngine;

// Tüm sesler kodla üretilir; dosya gerekmez.
public static class OgeAudio
{
    const int SR = 44100;
    public static AudioClip rifle, pistol, sniper, click, reload, hit, ding, step, swing, stab, hurt, draw, roundStart;
    static AudioSource src2D;
    static bool done;

    public static void Init()
    {
        if (done) return;
        done = true;
        rifle = Shot("rifle", 0.26f, 2200f, 1.0f, 110f, 1);
        pistol = Shot("pistol", 0.18f, 3200f, 0.85f, 170f, 2);
        sniper = Shot("sniper", 0.7f, 1400f, 1.0f, 60f, 3);
        click = Click("click", 3800f, 0.025f, 4);
        draw = Click("draw", 2400f, 0.06f, 5);
        reload = Reload();
        hit = Tone("hit", 1500, 1900, 0.06f, 0.45f);
        ding = Tone("ding", 2300, 2250, 0.28f, 0.4f);
        step = Noise("step", 0.09f, 600f, 0.5f, 6);
        swing = Swish("swing", 0.24f, 7);
        stab = Swish("stab", 0.32f, 8);
        hurt = Tone("hurt", 240, 110, 0.2f, 0.5f);
        roundStart = Tone("round", 660, 990, 0.35f, 0.35f);
    }

    static AudioClip Make(string n, float[] d)
    {
        var c = AudioClip.Create(n, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip Shot(string n, float len, float cut, float vol, float boom, int seed)
    {
        int N = (int)(len * SR); var d = new float[N]; var r = new System.Random(seed);
        float lp = 0, a = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
        for (int i = 0; i < N; i++)
        {
            float t = (float)i / SR;
            float x = (float)(r.NextDouble() * 2 - 1);
            lp += a * (x - lp);
            float crack = x * Mathf.Exp(-t * 90f) * 0.5f;
            float body = lp * Mathf.Exp(-t * 4.5f / len);
            float thump = Mathf.Sin(2 * Mathf.PI * boom * t * (1 - t * 0.8f)) * Mathf.Exp(-t * 22f);
            d[i] = Mathf.Clamp((crack + body * 1.6f + thump * 0.9f) * vol, -1, 1);
        }
        return Make(n, d);
    }

    static float[] ClickData(float freq, float len, int seed)
    {
        int N = (int)(len * SR); var d = new float[N]; var r = new System.Random(seed);
        for (int i = 0; i < N; i++)
        {
            float t = (float)i / SR;
            d[i] = (Mathf.Sin(2 * Mathf.PI * freq * t) * 0.6f + (float)(r.NextDouble() * 2 - 1) * 0.4f) * Mathf.Exp(-t * 120f);
        }
        return d;
    }

    static AudioClip Click(string n, float f, float len, int seed) => Make(n, ClickData(f, len, seed));

    static AudioClip Reload()
    {
        int N = (int)(1.2f * SR); var d = new float[N];
        void Put(float at, float f, int s) { var c = ClickData(f, 0.06f, s); int o = (int)(at * SR); for (int i = 0; i < c.Length && o + i < N; i++) d[o + i] += c[i]; }
        Put(0.05f, 1800, 1); Put(0.35f, 1200, 2); Put(0.75f, 2600, 3); Put(0.95f, 3400, 4);
        return Make("reload", d);
    }

    static AudioClip Tone(string n, float f0, float f1, float len, float vol)
    {
        int N = (int)(len * SR); var d = new float[N]; float ph = 0;
        for (int i = 0; i < N; i++)
        {
            float t = (float)i / SR, f = Mathf.Lerp(f0, f1, t / len);
            ph += 2 * Mathf.PI * f / SR;
            d[i] = Mathf.Sin(ph) * vol * Mathf.Min(1, t * 200f) * Mathf.Exp(-t * 6f / len);
        }
        return Make(n, d);
    }

    static AudioClip Noise(string n, float len, float cut, float vol, int seed)
    {
        int N = (int)(len * SR); var d = new float[N]; var r = new System.Random(seed);
        float lp = 0, a = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
        for (int i = 0; i < N; i++)
        {
            float t = (float)i / SR;
            lp += a * ((float)(r.NextDouble() * 2 - 1) - lp);
            d[i] = lp * vol * 3f * Mathf.Exp(-t * 40f);
        }
        return Make(n, d);
    }

    static AudioClip Swish(string n, float len, int seed)
    {
        int N = (int)(len * SR); var d = new float[N]; var r = new System.Random(seed);
        float lp = 0, hp = 0, prev = 0;
        for (int i = 0; i < N; i++)
        {
            float t = (float)i / SR, k = t / len;
            float cut = Mathf.Lerp(900f, 4000f, Mathf.Sin(k * Mathf.PI));
            float a = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
            float x = (float)(r.NextDouble() * 2 - 1);
            lp += a * (x - lp);
            hp = 0.95f * (hp + lp - prev); prev = lp;
            d[i] = hp * 1.4f * Mathf.Sin(k * Mathf.PI);
        }
        return Make(n, d);
    }

    public static void Play2D(AudioClip c, float vol)
    {
        if (c == null) return;
        if (src2D == null)
        {
            var g = new GameObject("ses2D");
            Object.DontDestroyOnLoad(g);
            src2D = g.AddComponent<AudioSource>();
            src2D.spatialBlend = 0;
        }
        src2D.PlayOneShot(c, vol);
    }

    public static void Play3D(AudioClip c, Vector3 p, float vol, float maxDist = 90f)
    {
        if (c == null) return;
        var g = new GameObject("ses3D");
        g.transform.position = p;
        var s = g.AddComponent<AudioSource>();
        s.clip = c; s.volume = vol; s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 4f; s.maxDistance = maxDist;
        s.dopplerLevel = 0;
        s.Play();
        Object.Destroy(g, c.length + 0.1f);
    }
}
