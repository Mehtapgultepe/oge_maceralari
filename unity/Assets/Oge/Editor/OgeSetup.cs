using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Menü: Oge > 1) Sahneyi kur  —  haritayı, ışığı, gökyüzünü, efektleri ve ağ ayarlarını tek seferde hazırlar.
public static class OgeSetup
{
    const string ROOT = "Assets/Oge", G = "Assets/Oge/Generated", SCENE = "Assets/Oge/Oge1v1.unity";
    const int GN = 40; const float CS = 2f;
    static float X(int c) => (c - (GN - 1) / 2f) * CS;
    static float Z(int r) => (r - (GN - 1) / 2f) * CS;

    [MenuItem("Oge/1) Sahneyi kur")]
    public static void Setup()
    {
        var litS = Shader.Find("Universal Render Pipeline/Lit");
        var unlitS = Shader.Find("Universal Render Pipeline/Unlit");
        if (litS == null || GraphicsSettings.defaultRenderPipeline == null)
        {
            EditorUtility.DisplayDialog("Oge", "URP bulunamadı. Projeyi Unity Hub'da 'Universal 3D' şablonuyla oluştur.", "Tamam");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.IsValidFolder(G)) AssetDatabase.DeleteAsset(G);
        AssetDatabase.CreateFolder(ROOT, "Generated");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- dokular ----
        var grassT = Tex("cimen", 256, 11, (u, v, n, r) =>
        {
            float f = Fbm(u, v, 8, n); var c = Color.Lerp(new Color(0.30f, 0.52f, 0.2f), new Color(0.5f, 0.74f, 0.32f), f);
            return Jit(c, r, 0.04f);
        }, (px, s, r) => Strokes(px, s, r, 1400, new Color(0.26f, 0.46f, 0.17f), new Color(0.58f, 0.8f, 0.38f), 3, 7));
        var dirtT = Tex("toprak", 256, 12, (u, v, n, r) =>
        {
            float f = Fbm(u, v, 6, n); var c = Color.Lerp(new Color(0.69f, 0.57f, 0.4f), new Color(0.86f, 0.76f, 0.57f), f);
            return Jit(c, r, 0.035f);
        }, (px, s, r) => Dots(px, s, r, 260, new Color(0.6f, 0.5f, 0.38f), new Color(0.93f, 0.86f, 0.7f)));
        var leafT = Tex("yaprak", 256, 13, (u, v, n, r) =>
        {
            float f = Fbm(u, v, 16, n); var c = Color.Lerp(new Color(0.16f, 0.38f, 0.16f), new Color(0.36f, 0.62f, 0.27f), f);
            return Jit(c, r, 0.05f);
        }, (px, s, r) => Blobs(px, s, r, 700, new Color(0.12f, 0.3f, 0.12f), new Color(0.45f, 0.72f, 0.33f)));
        var woodT = Tex("tahta", 128, 14, (u, v, n, r) =>
        {
            float plank = Mathf.Repeat(v * 4f, 1f);
            float grain = Mathf.Sin(u * 60f + Fbm(u, v, 4, n) * 9f) * 0.5f + 0.5f;
            var c = Color.Lerp(new Color(0.6f, 0.42f, 0.25f), new Color(0.78f, 0.58f, 0.36f), grain * 0.6f + Fbm(u, v, 8, n) * 0.4f);
            if (plank < 0.04f || u < 0.05f || u > 0.95f) c *= 0.55f;
            return Jit(c, r, 0.02f);
        }, null);
        var barkT = Tex("kabuk", 128, 15, (u, v, n, r) =>
        {
            float s = Mathf.Sin(u * 40f + Fbm(u, v, 8, n) * 6f) * 0.5f + 0.5f;
            return Jit(Color.Lerp(new Color(0.33f, 0.23f, 0.15f), new Color(0.52f, 0.38f, 0.25f), s), r, 0.03f);
        }, null);

        // ---- malzemeler ----
        var baseLit = Mat("lit", litS, Color.white, null, 0.3f);
        var baseUnlit = Mat("unlit", unlitS, Color.white, null, 0);
        var grassM = Mat("cimen", litS, Color.white, grassT, 0.08f, new Vector2(100, 100));
        var dirtM = Mat("toprak", litS, Color.white, dirtT, 0.05f);
        var leafM = Mat("cit", litS, Color.white, leafT, 0.12f);
        var woodM = Mat("kasa", litS, Color.white, woodT, 0.15f);
        var barkM = Mat("govde", litS, Color.white, barkT, 0.05f);
        var crownM = Mat("tac", litS, Color.white, leafT, 0.12f, new Vector2(2, 2));
        var bladeM = Mat("ot", litS, new Color(0.42f, 0.66f, 0.3f), null, 0.05f);
        bladeM.SetFloat("_Cull", 0f);
        bladeM.doubleSidedGI = true;
        var cloudM = Mat("bulut", litS, Color.white, null, 0f);
        cloudM.SetColor("_EmissionColor", new Color(0.35f, 0.37f, 0.4f));
        cloudM.EnableKeyword("_EMISSION");
        var hillM = Mat("tepe", litS, new Color(0.55f, 0.78f, 0.45f), null, 0.05f);
        Color[] fc = { new Color(0.93f, 0.58f, 0.69f), Color.white, new Color(1f, 0.88f, 0.48f), new Color(0.8f, 0.79f, 0.96f) };
        var flowerM = new Material[4];
        for (int i = 0; i < 4; i++) flowerM[i] = Mat("cicek" + i, litS, fc[i], null, 0.3f);

        // ---- harita ----
        var g = Grid();
        var map = new GameObject("Harita");
        map.isStatic = true;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Zemin"; ground.transform.SetParent(map.transform);
        ground.transform.localScale = new Vector3(400, 1, 400); ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.GetComponent<Renderer>().sharedMaterial = grassM;

        // patikalar (dünya koordinatlı UV ile tek parça)
        var pm = new MB();
        for (int r = 0; r < GN; r++) for (int c = 0; c < GN; c++)
                if (g[r, c] == ',') pm.Top(X(c) - 1, X(c) + 1, Z(r) - 1, Z(r) + 1, 0.012f, 4f);
        Obj("Patika", map.transform, pm.Build("patika"), dirtM, false);

        // çitler
        var hm = new MB();
        var hedgeCols = new GameObject("Çit çarpışma"); hedgeCols.transform.SetParent(map.transform);
        Runs(g, '#', (c0, c1, r) =>
        {
            float x0 = X(c0) - 1, x1 = X(c1) + 1, z0 = Z(r) - 1, z1 = Z(r) + 1;
            hm.Box(x0, x1, 0, 3.4f, z0, z1, 2.5f);
            var bc = hedgeCols.AddComponent<BoxCollider>();
            bc.center = new Vector3((x0 + x1) / 2, 1.7f, (z0 + z1) / 2); bc.size = new Vector3(x1 - x0, 3.4f, z1 - z0);
        });
        Obj("Çit", map.transform, hm.Build("cit"), leafM, false);

        // kasalar, ağaçlar
        var rnd = new System.Random(7);
        var crowns = new List<(Vector3 p, float s)>();
        for (int r = 0; r < GN; r++) for (int c = 0; c < GN; c++)
            {
                char ch = g[r, c]; float x = X(c), z = Z(r);
                if (ch == 'C' || ch == 'c')
                {
                    float s = ch == 'C' ? 1.9f : 1.2f, h = ch == 'C' ? 2.2f : 1.2f;
                    var k = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    k.name = "Kasa"; k.transform.SetParent(map.transform);
                    k.transform.position = new Vector3(x, h / 2, z); k.transform.localScale = new Vector3(s, h, s);
                    k.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 8 - 4, 0);
                    k.GetComponent<Renderer>().sharedMaterial = woodM;
                }
                if (ch == 'T') { Tree(map.transform, new Vector3(x, 0, z), 1.2f, barkM, true); crowns.Add((new Vector3(x, 0, z), 1.2f)); }
            }
        for (int i = 0; i < 70; i++)
        {
            float a = i / 70f * Mathf.PI * 2 + (float)rnd.NextDouble() * 0.05f, rr = 46 + (float)rnd.NextDouble() * 34;
            var p = new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr); float s = 0.9f + (float)rnd.NextDouble() * 0.9f;
            Tree(map.transform, p, s, barkM, false); crowns.Add((p, s));
        }
        // ağaç tepeleri tek parça
        var cm = new MB();
        var sph = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        foreach (var (p, s) in crowns)
        {
            cm.AddMesh(sph, Matrix4x4.TRS(p + new Vector3(0, 4 * s, 0), Quaternion.identity, Vector3.one * 4f * s));
            cm.AddMesh(sph, Matrix4x4.TRS(p + new Vector3(0.9f * s, 4.8f * s, 0.4f * s), Quaternion.identity, Vector3.one * 2.8f * s));
            cm.AddMesh(sph, Matrix4x4.TRS(p + new Vector3(-0.9f * s, 4.4f * s, -0.5f * s), Quaternion.identity, Vector3.one * 2.6f * s));
        }
        Obj("Ağaç tepeleri", map.transform, cm.Build("tac"), crownM, false);

        // otlar ve çiçekler (açık alanlarda)
        var bm = new MB(); var fms = new MB[4]; for (int i = 0; i < 4; i++) fms[i] = new MB();
        for (int r = 0; r < GN; r++) for (int c = 0; c < GN; c++)
            {
                if (g[r, c] != '.') continue;
                for (int k = 0; k < 7; k++)
                {
                    var p = new Vector3(X(c) + (float)rnd.NextDouble() * 2 - 1, 0, Z(r) + (float)rnd.NextDouble() * 2 - 1);
                    for (int b = 0; b < 3; b++) bm.Blade(p + new Vector3((float)rnd.NextDouble() * 0.2f - 0.1f, 0, (float)rnd.NextDouble() * 0.2f - 0.1f), (float)rnd.NextDouble() * 6.3f, 0.18f + (float)rnd.NextDouble() * 0.28f, rnd);
                }
                if (rnd.NextDouble() < 0.35)
                {
                    var p = new Vector3(X(c) + (float)rnd.NextDouble() * 1.8f - 0.9f, 0.12f, Z(r) + (float)rnd.NextDouble() * 1.8f - 0.9f);
                    fms[rnd.Next(4)].AddMesh(sph, Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * 0.16f));
                }
            }
        var grassObj = Obj("Otlar", map.transform, bm.Build("ot"), bladeM, false);
        grassObj.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        for (int i = 0; i < 4; i++) Obj("Çiçek" + i, map.transform, fms[i].Build("cicek" + i), flowerM[i], false);

        // tepeler ve bulutlar
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2;
            var h = GameObject.CreatePrimitive(PrimitiveType.Sphere); h.name = "Tepe"; h.transform.SetParent(map.transform);
            Object.DestroyImmediate(h.GetComponent<Collider>());
            float s = 60 + (float)rnd.NextDouble() * 30;
            h.transform.position = new Vector3(Mathf.Cos(a) * 120, -6, Mathf.Sin(a) * 120); h.transform.localScale = new Vector3(s, s * 0.32f, s);
            h.GetComponent<Renderer>().sharedMaterial = hillM;
        }
        var clm = new MB();
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2; var cp = new Vector3(Mathf.Cos(a) * 95, 42 + (float)rnd.NextDouble() * 12, Mathf.Sin(a) * 95);
            for (int k = 0; k < 5; k++) clm.AddMesh(sph, Matrix4x4.TRS(cp + new Vector3(k * 5 - 10, (float)rnd.NextDouble() * 2, (float)rnd.NextDouble() * 4), Quaternion.identity, Vector3.one * (8 + (float)rnd.NextDouble() * 6)));
        }
        var clouds = Obj("Bulutlar", map.transform, clm.Build("bulut"), cloudM, false);
        clouds.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        // ---- ışık, gökyüzü, sis ----
        var sunGo = new GameObject("Güneş");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.95f, 0.85f); sun.intensity = 1.55f;
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f;
        sunGo.transform.rotation = Quaternion.Euler(48, -35, 0);
        RenderSettings.sun = sun;
        var sky = new Material(Shader.Find("Skybox/Procedural"));
        sky.SetFloat("_SunSize", 0.035f); sky.SetFloat("_AtmosphereThickness", 0.75f);
        sky.SetColor("_SkyTint", new Color(0.42f, 0.62f, 1f)); sky.SetColor("_GroundColor", new Color(0.55f, 0.7f, 0.5f)); sky.SetFloat("_Exposure", 1.3f);
        AssetDatabase.CreateAsset(sky, G + "/gokyuzu.mat");
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.73f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.62f, 0.55f);
        RenderSettings.ambientGroundColor = new Color(0.33f, 0.38f, 0.28f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 55; RenderSettings.fogEndDistance = 170;
        RenderSettings.fogColor = new Color(0.78f, 0.88f, 0.97f);

        // ---- görüntü efektleri ----
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, G + "/efektler.asset");
        var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.45f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.6f);
        var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
        var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(14f); ca.contrast.Override(8f); ca.postExposure.Override(0.1f);
        var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.2f); vg.smoothness.Override(0.45f);
        foreach (var comp in prof.components) AssetDatabase.AddObjectToAsset(comp, prof);
        var vol = new GameObject("Efektler").AddComponent<Volume>();
        vol.isGlobal = true; vol.sharedProfile = prof;

        // ---- kameralar ----
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400; cam.cullingMask = ~(1 << OgeArt.VMLayer);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0, 24, 48); camGo.transform.LookAt(Vector3.zero);
        var cd = cam.GetUniversalAdditionalCameraData();
        cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var vmGo = new GameObject("Silah kamerası"); vmGo.transform.SetParent(camGo.transform, false);
        var vc = vmGo.AddComponent<Camera>();
        vc.fieldOfView = 58; vc.nearClipPlane = 0.01f; vc.farClipPlane = 5; vc.cullingMask = 1 << OgeArt.VMLayer;
        var vcd = vc.GetUniversalAdditionalCameraData(); vcd.renderType = CameraRenderType.Overlay;
        cd.cameraStack.Add(vc);
        var vmRoot = new GameObject("Elde silah"); vmRoot.transform.SetParent(vmGo.transform, false);
        vmRoot.AddComponent<OgeViewModel>();

        var art = new GameObject("OgeArt").AddComponent<OgeArt>();
        art.lit = baseLit; art.unlit = baseUnlit; art.mainCam = cam; art.vmCam = vc;

        // ---- ağ ----
        var pg = new GameObject("OgePlayer");
        pg.AddComponent<NetworkObject>();
        var ccc = pg.AddComponent<CharacterController>();
        ccc.height = 1.75f; ccc.radius = 0.35f; ccc.center = new Vector3(0, 0.875f, 0); ccc.stepOffset = 0.35f; ccc.skinWidth = 0.04f;
        pg.AddComponent<OgePlayer>();
        var playerPrefab = PrefabUtility.SaveAsPrefabAsset(pg, G + "/OgePlayer.prefab");
        Object.DestroyImmediate(pg);
        var sg = new GameObject("OgeGameState");
        sg.AddComponent<NetworkObject>(); sg.AddComponent<OgeGameState>();
        var statePrefab = PrefabUtility.SaveAsPrefabAsset(sg, G + "/OgeGameState.prefab");
        Object.DestroyImmediate(sg);

        var ng = new GameObject("NetworkManager");
        var nm = ng.AddComponent<NetworkManager>();
        var ut = ng.AddComponent<UnityTransport>();
        ut.UseWebSockets = true;
        if (nm.NetworkConfig == null) nm.NetworkConfig = new NetworkConfig();
        nm.NetworkConfig.NetworkTransport = ut;
        nm.NetworkConfig.PlayerPrefab = playerPrefab;
        var on = ng.AddComponent<OgeNet>();
        on.statePrefab = statePrefab;
        EditorUtility.SetDirty(nm);

        // ---- kaydet + ayarlar ----
        EditorSceneManager.SaveScene(scene, SCENE);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE, true) };
        PlayerSettings.productName = "Oge Macerası 1v1";
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        var assets = new List<RenderPipelineAsset> { GraphicsSettings.defaultRenderPipeline };
        for (int i = 0; i < QualitySettings.names.Length; i++) assets.Add(QualitySettings.GetRenderPipelineAssetAt(i));
        foreach (var a in assets)
            if (a is UniversalRenderPipelineAsset u) { u.shadowDistance = 90f; u.msaaSampleCount = 4; EditorUtility.SetDirty(u); }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Oge", "Sahne hazır! Play'e basıp deneyebilirsin. Sonra Oge > 2) WebGL olarak derle.", "Süper");
    }

    [MenuItem("Oge/2) WebGL olarak derle")]
    public static void BuildWebGL()
    {
        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "WebGLBuild");
        var rep = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { SCENE }, locationPathName = path, target = BuildTarget.WebGL, options = BuildOptions.None });
        if (rep.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorUtility.RevealInFinder(path);
        else EditorUtility.DisplayDialog("Oge", "Derleme başarısız. Console'daki kırmızı hataları kopyalayıp gönder.", "Tamam");
    }

    // ---------- harita (tarayıcıdaki Çayır ile aynı) ----------
    static char[,] Grid()
    {
        var g = new char[GN, GN];
        for (int r = 0; r < GN; r++) for (int c = 0; c < GN; c++) g[r, c] = '#';
        void Carve(int c0, int c1, int r0, int r1, char ch = ',') { for (int r = r0; r <= r1; r++) for (int c = c0; c <= c1; c++) g[r, c] = ch; }
        Carve(2, 37, 30, 37, '.'); Carve(3, 9, 12, 29); Carve(17, 22, 9, 29); Carve(30, 36, 12, 29);
        Carve(2, 14, 2, 11, '.'); Carve(25, 37, 2, 11, '.'); Carve(15, 24, 2, 8, '.');
        for (int r = 2; r <= 4; r++) { g[r, 14] = '#'; g[r, 25] = '#'; }
        int[,] P = { {5,18,'c'},{7,24,'C'},{19,20,'C'},{21,14,'c'},{33,17,'c'},{32,24,'C'},{6,6,'C'},{7,6,'C'},{11,9,'c'},{4,9,'c'},{12,3,'T'},
                     {32,6,'C'},{33,6,'C'},{28,9,'c'},{35,9,'c'},{36,3,'T'},{4,36,'T'},{35,36,'T'},{12,32,'c'},{27,32,'c'},{16,3,'T'},{23,3,'T'} };
        for (int i = 0; i < P.GetLength(0); i++) g[P[i, 1], P[i, 0]] = (char)P[i, 2];
        return g;
    }

    static void Runs(char[,] g, char ch, System.Action<int, int, int> fn)
    {
        for (int r = 0; r < GN; r++)
        {
            int c = 0;
            while (c < GN)
            {
                if (g[r, c] == ch) { int e = c; while (e + 1 < GN && g[r, e + 1] == ch) e++; fn(c, e, r); c = e + 1; }
                else c++;
            }
        }
    }

    static void Tree(Transform parent, Vector3 p, float s, Material bark, bool col)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        t.name = "Ağaç"; t.transform.SetParent(parent);
        t.transform.position = p + new Vector3(0, 1.5f * s, 0); t.transform.localScale = new Vector3(0.7f * s, 1.5f * s, 0.7f * s);
        t.GetComponent<Renderer>().sharedMaterial = bark;
        if (!col) Object.DestroyImmediate(t.GetComponent<Collider>());
        t.isStatic = true;
    }

    static GameObject Obj(string name, Transform parent, Mesh m, Material mat, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = m;
        go.isStatic = true;
        return go;
    }

    static Material Mat(string n, Shader s, Color c, Texture2D t, float smooth, Vector2? tile = null)
    {
        var m = new Material(s);
        m.SetColor("_BaseColor", c);
        if (t != null) { m.SetTexture("_BaseMap", t); if (tile != null) m.SetTextureScale("_BaseMap", tile.Value); }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        m.enableInstancing = true;
        AssetDatabase.CreateAsset(m, G + "/" + n + ".mat");
        return m;
    }

    // ---------- doku üretimi ----------
    static Texture2D Tex(string name, int s, int seed, System.Func<float, float, float[], System.Random, Color> f, System.Action<Color[], int, System.Random> post)
    {
        var r = new System.Random(seed);
        var lat = new float[64 * 64]; for (int i = 0; i < lat.Length; i++) lat[i] = (float)r.NextDouble();
        var px = new Color[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) px[y * s + x] = f((float)x / s, (float)y / s, lat, r);
        post?.Invoke(px, s, r);
        var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, name = name };
        t.SetPixels(px); t.Apply(true);
        AssetDatabase.CreateAsset(t, G + "/" + name + ".asset");
        return t;
    }

    // döşenebilir değer gürültüsü
    static float VN(float u, float v, int n, float[] lat)
    {
        float x = u * n, y = v * n; int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float L(int a, int b) => lat[((b % n + n) % n) * 64 + ((a % n + n) % n)];
        return Mathf.Lerp(Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), fx), Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), fx), fy);
    }

    static float Fbm(float u, float v, int n, float[] lat) =>
        VN(u, v, n, lat) * 0.55f + VN(u, v, Mathf.Min(n * 2, 64), lat) * 0.3f + VN(u, v, Mathf.Min(n * 4, 64), lat) * 0.15f;

    static Color Jit(Color c, System.Random r, float a)
    {
        float j = ((float)r.NextDouble() - 0.5f) * 2 * a;
        return new Color(Mathf.Clamp01(c.r + j), Mathf.Clamp01(c.g + j), Mathf.Clamp01(c.b + j), 1);
    }

    static void Strokes(Color[] px, int s, System.Random r, int n, Color a, Color b, int l0, int l1)
    {
        for (int i = 0; i < n; i++)
        {
            int x = r.Next(s), y = r.Next(s), len = r.Next(l0, l1); var c = r.NextDouble() < 0.5 ? a : b;
            int dx = r.Next(-1, 2);
            for (int k = 0; k < len; k++) { int xx = ((x + dx * k / 3) % s + s) % s, yy = (y + k) % s; px[yy * s + xx] = Color.Lerp(px[yy * s + xx], c, 0.6f); }
        }
    }

    static void Dots(Color[] px, int s, System.Random r, int n, Color a, Color b)
    {
        for (int i = 0; i < n; i++)
        {
            int x = r.Next(s), y = r.Next(s), rad = r.Next(1, 3); var c = r.NextDouble() < 0.5 ? a : b;
            for (int yy = -rad; yy <= rad; yy++) for (int xx = -rad; xx <= rad; xx++)
                    if (xx * xx + yy * yy <= rad * rad) { int X2 = ((x + xx) % s + s) % s, Y2 = ((y + yy) % s + s) % s; px[Y2 * s + X2] = Color.Lerp(px[Y2 * s + X2], c, 0.7f); }
        }
    }

    static void Blobs(Color[] px, int s, System.Random r, int n, Color a, Color b)
    {
        for (int i = 0; i < n; i++)
        {
            int x = r.Next(s), y = r.Next(s), rx = r.Next(2, 6), ry = r.Next(1, 4); var c = r.NextDouble() < 0.5 ? a : b;
            for (int yy = -ry; yy <= ry; yy++) for (int xx = -rx; xx <= rx; xx++)
                    if ((float)xx * xx / (rx * rx) + (float)yy * yy / (ry * ry) <= 1) { int X2 = ((x + xx) % s + s) % s, Y2 = ((y + yy) % s + s) % s; px[Y2 * s + X2] = Color.Lerp(px[Y2 * s + X2], c, 0.5f); }
        }
    }

    // ---------- birleşik mesh yapıcı ----------
    class MB
    {
        readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();

        void Q(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nn, System.Func<Vector3, Vector2> f)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) n.Add(nn);
            uv.Add(f(a)); uv.Add(f(b)); uv.Add(f(c)); uv.Add(f(d));
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        public void Top(float x0, float x1, float z0, float z1, float y, float sc)
        {
            Q(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), Vector3.up, p => new Vector2(p.x / sc, p.z / sc));
        }

        public void Box(float x0, float x1, float y0, float y1, float z0, float z1, float sc)
        {
            System.Func<Vector3, Vector2> fx = p => new Vector2(p.z / sc, p.y / sc), fz = p => new Vector2(p.x / sc, p.y / sc), fy = p => new Vector2(p.x / sc, p.z / sc);
            Q(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), Vector3.up, fy);
            Q(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1), Vector3.right, fx);
            Q(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0), Vector3.left, fx);
            Q(new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1), Vector3.forward, fz);
            Q(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0), Vector3.back, fz);
        }

        public void Blade(Vector3 p, float ang, float h, System.Random r)
        {
            var side = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.035f;
            var lean = new Vector3((float)r.NextDouble() - 0.5f, 0, (float)r.NextDouble() - 0.5f) * 0.25f * h;
            int i = v.Count;
            v.Add(p - side); v.Add(p + side); v.Add(p + Vector3.up * h + lean);
            for (int k = 0; k < 3; k++) { n.Add(Vector3.up); }
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0.5f, 1));
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public void AddMesh(Mesh m, Matrix4x4 M)
        {
            int i0 = v.Count;
            var mv = m.vertices; var mn = m.normals; var mu = m.uv;
            for (int i = 0; i < mv.Length; i++) { v.Add(M.MultiplyPoint3x4(mv[i])); n.Add(M.MultiplyVector(mn[i]).normalized); uv.Add(mu.Length > i ? mu[i] : Vector2.zero); }
            foreach (var k in m.triangles) t.Add(i0 + k);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, G + "/" + name + ".asset");
            return m;
        }
    }
}
