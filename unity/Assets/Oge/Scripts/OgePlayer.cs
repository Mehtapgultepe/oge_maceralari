using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyuncu: kendi bilgisayarında hareket + ateş; rakibin ekranında model olarak görünür.
public class OgePlayer : NetworkBehaviour
{
    public static readonly List<OgePlayer> All = new List<OgePlayer>();
    public static OgePlayer Local;

    public NetworkVariable<Vector3> nPos = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> nLook = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<byte> nWep = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> nName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> hp = new NetworkVariable<int>(100);

    public int Team => OwnerClientId == 0 ? 0 : 1;
    public string Name => nName.Value.ToString();
    public bool Alive => hp.Value > 0;
    public float Yaw => IsOwner ? yaw : nLook.Value.x;
    public Color TeamColor => Team == 0 ? OgeArt.Red : OgeArt.Blue;

    CharacterController cc;
    Transform model, headT, gunT, knifeT;
    readonly List<Collider> hitCols = new List<Collider>();
    readonly List<Renderer> rends = new List<Renderer>();

    // yerel durum
    float yaw, pitch, vy, kickPitch, sens;
    bool grounded, moving;
    Vector2 lookDelta;
    int w;
    readonly int[] ammo = { 30, 12, 5, 0 };
    float cool, relT, relDur = 1, drawT, swT, swDur = 1, insT, insDur = 1, recoil;
    bool heavyNow, zoom;
    float hitT, hurtT, stepT;
    bool hitHead;
    // uzak oyuncu
    float deadAng, rStepT;
    Vector3 lastPos;

    static Texture2D scopeTex;

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        cc = GetComponent<CharacterController>();
        BuildModel();
        hp.OnValueChanged += OnHp;
        if (IsOwner)
        {
            Local = this;
            string n = string.IsNullOrEmpty(OgeNet.MyName) ? (Team == 0 ? "Kırmızı" : "Mavi") : OgeNet.MyName;
            if (n.Length > 12) n = n.Substring(0, 12);
            nName.Value = new FixedString32Bytes(n);
            sens = PlayerPrefs.GetFloat("oge_sens", 0.09f);
            foreach (var c in hitCols) c.enabled = false;
            foreach (var r in rends) r.enabled = false;
            Teleport(OgeArt.Spawn(Team), OgeArt.SpawnYaw(Team));
            OgeViewModel.I.Show(true);
            OgeViewModel.I.SetWeapon(0, TeamColor);
            drawT = 0.45f;
        }
        else
        {
            transform.position = nPos.Value;
            lastPos = transform.position;
        }
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        hp.OnValueChanged -= OnHp;
        if (Local == this)
        {
            Local = null;
            if (OgeViewModel.I) OgeViewModel.I.Show(false);
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void OnHp(int o, int n)
    {
        if (n < o)
        {
            if (IsOwner) { hurtT = 0.5f; OgeAudio.Play2D(OgeAudio.hurt, 0.6f); }
            else if (n > 0) OgeArt.I.StartCoroutine(OgeArt.I.Puff(transform.position + Vector3.up * 1.2f, new Color(0.85f, 0.2f, 0.2f), 0.4f, 0.25f));
        }
    }

    // ---------- model (rakibin gördüğü) ----------
    void BuildModel()
    {
        var A = OgeArt.I;
        model = new GameObject("Model").transform;
        model.SetParent(transform, false);
        Material team = A.LitC(TeamColor, 0.25f), dark = A.LitC(new Color(0.2f, 0.22f, 0.27f), 0.4f), skin = A.LitC(new Color(0.96f, 0.78f, 0.62f), 0.2f);
        Material pants = A.LitC(new Color(0.26f, 0.29f, 0.38f), 0.15f), shoe = A.LitC(new Color(0.15f, 0.14f, 0.16f), 0.3f), white = A.LitC(Color.white, 0.6f);

        Hit(OgeArt.Prim(PrimitiveType.Capsule, model, new Vector3(-0.13f, 0.42f, 0), new Vector3(0.22f, 0.42f, 0.22f), pants, default, true), false);
        Hit(OgeArt.Prim(PrimitiveType.Capsule, model, new Vector3(0.13f, 0.42f, 0), new Vector3(0.22f, 0.42f, 0.22f), pants, default, true), false);
        Keep(OgeArt.Prim(PrimitiveType.Cube, model, new Vector3(-0.13f, 0.05f, 0.05f), new Vector3(0.2f, 0.1f, 0.3f), shoe));
        Keep(OgeArt.Prim(PrimitiveType.Cube, model, new Vector3(0.13f, 0.05f, 0.05f), new Vector3(0.2f, 0.1f, 0.3f), shoe));
        Hit(OgeArt.Prim(PrimitiveType.Capsule, model, new Vector3(0, 1.12f, 0), new Vector3(0.56f, 0.38f, 0.38f), team, default, true), false);

        headT = new GameObject("Kafa").transform;
        headT.SetParent(model, false);
        headT.localPosition = new Vector3(0, 1.58f, 0);
        Hit(OgeArt.Prim(PrimitiveType.Sphere, headT, Vector3.zero, Vector3.one * 0.32f, skin, default, true), true);
        Keep(OgeArt.Prim(PrimitiveType.Sphere, headT, new Vector3(0, 0.07f, -0.01f), new Vector3(0.34f, 0.22f, 0.34f), team));
        Keep(OgeArt.Prim(PrimitiveType.Cube, headT, new Vector3(0, 0.1f, 0.15f), new Vector3(0.26f, 0.03f, 0.14f), team));
        Keep(OgeArt.Prim(PrimitiveType.Sphere, headT, new Vector3(-0.06f, 0.01f, 0.15f), Vector3.one * 0.05f, A.LitC(new Color(0.12f, 0.12f, 0.16f), 0.8f)));
        Keep(OgeArt.Prim(PrimitiveType.Sphere, headT, new Vector3(0.06f, 0.01f, 0.15f), Vector3.one * 0.05f, A.LitC(new Color(0.12f, 0.12f, 0.16f), 0.8f)));

        Keep(OgeArt.Prim(PrimitiveType.Capsule, model, new Vector3(0.3f, 1.2f, 0.22f), new Vector3(0.14f, 0.24f, 0.14f), team, new Vector3(70, 0, 0)));
        Keep(OgeArt.Prim(PrimitiveType.Capsule, model, new Vector3(-0.22f, 1.2f, 0.3f), new Vector3(0.14f, 0.26f, 0.14f), team, new Vector3(70, 30, 0)));
        Keep(OgeArt.Prim(PrimitiveType.Sphere, model, new Vector3(0.22f, 1.18f, 0.42f), Vector3.one * 0.12f, skin));
        Keep(OgeArt.Prim(PrimitiveType.Sphere, model, new Vector3(0.0f, 1.2f, 0.55f), Vector3.one * 0.12f, skin));

        gunT = new GameObject("Silah").transform;
        gunT.SetParent(model, false);
        gunT.localPosition = new Vector3(0.12f, 1.25f, 0.5f);
        Keep(OgeArt.Prim(PrimitiveType.Cube, gunT, Vector3.zero, new Vector3(0.07f, 0.1f, 0.6f), dark));
        Keep(OgeArt.Prim(PrimitiveType.Cube, gunT, new Vector3(0, -0.1f, 0.02f), new Vector3(0.05f, 0.14f, 0.07f), dark));
        knifeT = new GameObject("Bıçak").transform;
        knifeT.SetParent(model, false);
        knifeT.localPosition = new Vector3(0.22f, 1.18f, 0.52f);
        Keep(OgeArt.Prim(PrimitiveType.Cube, knifeT, new Vector3(0, 0, 0.08f), new Vector3(0.02f, 0.03f, 0.18f), white));
        knifeT.gameObject.SetActive(false);
    }

    void Hit(GameObject g, bool head)
    {
        var c = g.GetComponent<Collider>();
        c.isTrigger = true;
        hitCols.Add(c);
        var h = g.AddComponent<OgeHit>();
        h.owner = this; h.head = head;
        Keep(g);
    }

    void Keep(GameObject g) { rends.Add(g.GetComponent<Renderer>()); }

    // ---------- döngü ----------
    void Update()
    {
        if (!IsSpawned) return;
        float dt = Time.deltaTime;
        if (IsOwner) OwnerUpdate(dt); else RemoteUpdate(dt);
    }

    void RemoteUpdate(float dt)
    {
        Vector3 tp = nPos.Value;
        if ((tp - transform.position).sqrMagnitude > 25f) transform.position = tp;
        else transform.position = Vector3.Lerp(transform.position, tp, 1 - Mathf.Exp(-16f * dt));
        transform.rotation = Quaternion.Euler(0, nLook.Value.x, 0);
        headT.localRotation = Quaternion.Euler(Mathf.Clamp(nLook.Value.y, -40, 40), 0, 0);
        gunT.localRotation = Quaternion.Euler(nLook.Value.y * 0.6f, 0, 0);
        deadAng = Mathf.MoveTowards(deadAng, Alive ? 0 : -88f, dt * 260f);
        model.localRotation = Quaternion.Euler(deadAng, 0, 0);
        int wi = nWep.Value;
        gunT.gameObject.SetActive(wi < 3);
        knifeT.gameObject.SetActive(wi == 3);

        Vector3 d = transform.position - lastPos; d.y = 0;
        float spd = d.magnitude / Mathf.Max(dt, 0.0001f);
        lastPos = transform.position;
        if (spd > 3.5f && Alive)
        {
            rStepT -= dt;
            if (rStepT <= 0) { rStepT = 0.38f; OgeAudio.Play3D(OgeAudio.step, transform.position, 0.55f, 30f); }
        }
    }

    int Phase => OgeGameState.I ? OgeGameState.I.phase.Value : 0;

    void OwnerUpdate(float dt)
    {
        var kb = Keyboard.current; var ms = Mouse.current;
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        bool canMove = Alive && Phase != 3;
        bool canAct = Alive && (Phase == 0 || Phase == 1) && locked;

        // bakış
        lookDelta = Vector2.zero;
        if (locked && ms != null)
        {
            lookDelta = ms.delta.ReadValue();
            float s = sens * (zoom ? 0.35f : 1f);
            yaw += lookDelta.x * s;
            pitch = Mathf.Clamp(pitch - lookDelta.y * s, -85f, 85f);
        }

        // hareket
        Vector2 mv = Vector2.zero;
        bool walk = false;
        if (locked && canMove && kb != null)
        {
            if (kb.wKey.isPressed) mv.y += 1;
            if (kb.sKey.isPressed) mv.y -= 1;
            if (kb.dKey.isPressed) mv.x += 1;
            if (kb.aKey.isPressed) mv.x -= 1;
            walk = kb.leftShiftKey.isPressed;
        }
        float spd = (walk ? 2.8f : 5.8f) * (zoom ? 0.6f : 1f) * (w == 3 ? 1.1f : 1f);
        var rot = Quaternion.Euler(0, yaw, 0);
        Vector3 h = rot * new Vector3(mv.x, 0, mv.y);
        if (h.sqrMagnitude > 1) h.Normalize();
        h *= spd;
        grounded = cc.isGrounded;
        if (grounded && vy < 0) vy = -1f;
        if (grounded && locked && canMove && kb != null && kb.spaceKey.wasPressedThisFrame) vy = 6.2f;
        vy -= 18f * dt;
        cc.Move((h + Vector3.up * vy) * dt);
        transform.rotation = rot;
        moving = h.sqrMagnitude > 1f && grounded;
        if (moving && !walk) { stepT -= dt; if (stepT <= 0) { stepT = 0.38f; OgeAudio.Play2D(OgeAudio.step, 0.22f); } }
        if (transform.position.y < -10f) Teleport(OgeArt.Spawn(Team), OgeArt.SpawnYaw(Team));

        // silah zamanlayıcıları
        var W = OgeWeapons.W[w];
        cool -= dt; drawT -= dt; swT -= dt; insT -= dt; hitT -= dt; hurtT -= dt;
        if (relT > 0) { relT -= dt; if (relT <= 0) ammo[w] = W.mag; }
        recoil = Mathf.MoveTowards(recoil, 0, dt * 0.2f);
        kickPitch = Mathf.Lerp(kickPitch, 0, 1 - Mathf.Exp(-7f * dt));

        if (canAct && kb != null && ms != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) Swap(0);
            if (kb.digit2Key.wasPressedThisFrame) Swap(1);
            if (kb.digit3Key.wasPressedThisFrame) Swap(2);
            if (kb.digit4Key.wasPressedThisFrame) Swap(3);
            float sc = ms.scroll.ReadValue().y;
            if (sc > 0.1f) Swap((w + 3) % 4); else if (sc < -0.1f) Swap((w + 1) % 4);
            if (kb.rKey.wasPressedThisFrame) Reload();
            if (kb.yKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) Inspect();
            W = OgeWeapons.W[w];
            if (W.melee)
            {
                if (ms.leftButton.isPressed) Melee(false);
                else if (ms.rightButton.isPressed) Melee(true);
            }
            else
            {
                if (W.auto ? ms.leftButton.isPressed : ms.leftButton.wasPressedThisFrame) Fire();
                if (W.scope && ms.rightButton.wasPressedThisFrame && relT <= 0) { zoom = !zoom; OgeAudio.Play2D(OgeAudio.click, 0.3f); }
            }
        }
        if (relT > 0 || !Alive) zoom = false;

        nPos.Value = transform.position;
        nLook.Value = new Vector2(yaw, pitch);
        nWep.Value = (byte)w;

        // kamera
        var cam = OgeArt.I.mainCam;
        Vector3 eye = transform.position + Vector3.up * (Alive ? 1.6f : 0.45f);
        cam.transform.SetPositionAndRotation(eye, Quaternion.Euler(pitch - kickPitch, yaw, Alive ? 0 : 25f));
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoom ? 22f : 75f, 1 - Mathf.Exp(-20f * dt));

        OgeViewModel.I.Tick(dt, moving, lookDelta,
            drawT > 0 ? drawT / 0.45f : 0,
            relT > 0 ? 1 - relT / relDur : -1,
            swT > 0 ? 1 - swT / swDur : -1, heavyNow,
            insT > 0 ? 1 - insT / insDur : -1,
            zoom || !Alive, w == 3);

        // ekrana tıklayınca fare kilitlenir
        if (!locked && ms != null && ms.leftButton.wasPressedThisFrame && Phase != 3 && !menuHover) Cursor.lockState = CursorLockMode.Locked;
    }

    public void Teleport(Vector3 p, float y)
    {
        cc.enabled = false;
        transform.position = p;
        cc.enabled = true;
        yaw = y; pitch = 0; vy = 0;
        if (IsOwner) nPos.Value = p;
    }

    void ResetLoadout()
    {
        ammo[0] = 30; ammo[1] = 12; ammo[2] = 5; ammo[3] = 0;
        relT = 0; insT = 0; zoom = false; drawT = 0.45f; cool = 0.3f; recoil = 0;
    }

    void Swap(int i)
    {
        if (i == w) return;
        w = i; relT = 0; zoom = false; insT = 0; drawT = 0.45f; cool = Mathf.Max(cool, 0.3f);
        OgeViewModel.I.SetWeapon(w, TeamColor);
        OgeAudio.Play2D(OgeAudio.draw, 0.4f);
    }

    void Inspect()
    {
        if (relT > 0 || insT > 0 || swT > 0) return;
        zoom = false;
        insT = insDur = w == 3 ? 2.2f : 1.8f;
    }

    void Reload()
    {
        var W = OgeWeapons.W[w];
        if (W.melee || relT > 0 || ammo[w] >= W.mag) return;
        relT = relDur = W.rel; zoom = false; insT = 0;
        OgeAudio.Play2D(OgeAudio.reload, 0.5f);
    }

    bool Trace(Vector3 o, Vector3 d, float range, out RaycastHit best, out OgePlayer target, out bool head)
    {
        target = null; head = false; best = default;
        var hits = Physics.RaycastAll(o, d, range, ~(1 << OgeArt.VMLayer), QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hh in hits)
        {
            if (hh.collider is CharacterController) continue;
            var hb = hh.collider.GetComponent<OgeHit>();
            if (hb != null)
            {
                if (hb.owner == this || !hb.owner.Alive) continue;
                best = hh; target = hb.owner; head = hb.head;
                return true;
            }
            if (hh.collider.isTrigger) continue;
            best = hh;
            return true;
        }
        return false;
    }

    void Fire()
    {
        var W = OgeWeapons.W[w];
        if (cool > 0 || relT > 0 || drawT > 0.15f) return;
        if (ammo[w] <= 0) { OgeAudio.Play2D(OgeAudio.click, 0.5f); cool = 0.25f; Reload(); return; }
        ammo[w]--; cool = W.rate; insT = 0;
        float sp = W.spread + (moving || !grounded ? W.move : 0) + recoil * 0.5f;
        if (W.scope && zoom) sp = moving ? 0.03f : 0f;
        var ct = OgeArt.I.mainCam.transform;
        Vector3 dir = (ct.forward + ct.right * Random.Range(-sp, sp) + ct.up * Random.Range(-sp, sp)).normalized;
        Vector3 end = ct.position + dir * 120f;
        if (Trace(ct.position, dir, 120f, out var hit, out var t, out bool head))
        {
            end = hit.point;
            if (t != null)
            {
                int dmg = Mathf.RoundToInt(W.dmg * (head ? W.head : 1f));
                HitServerRpc(t.NetworkObjectId, dmg);
                hitT = 0.25f; hitHead = head;
                OgeAudio.Play2D(head ? OgeAudio.ding : OgeAudio.hit, 0.55f);
            }
            else OgeArt.I.Impact(hit.point, hit.normal);
        }
        recoil = Mathf.Min(recoil + W.kick, 0.12f);
        kickPitch += W.kick * 55f;
        OgeAudio.Play2D(w == 0 ? OgeAudio.rifle : w == 1 ? OgeAudio.pistol : OgeAudio.sniper, w == 2 ? 0.9f : 0.7f);
        OgeViewModel.I.Kick(W.kick);
        OgeViewModel.I.Flash();
        Vector3 from = ct.position + ct.right * 0.15f - ct.up * 0.12f + ct.forward * 0.6f;
        OgeArt.I.Tracer(from, end);
        OgeArt.I.MuzzleLight(from);
        ShotServerRpc(end, (byte)w);
        if (W.scope) zoom = false;
    }

    void Melee(bool heavy)
    {
        var W = OgeWeapons.W[w];
        if (cool > 0 || drawT > 0.1f) return;
        cool = heavy ? W.heavyRate : W.rate;
        swT = swDur = heavy ? 0.45f : 0.3f;
        heavyNow = heavy; insT = 0;
        OgeAudio.Play2D(OgeAudio.swing, 0.5f);
        var ct = OgeArt.I.mainCam.transform;
        if (Trace(ct.position, ct.forward, W.range, out var hit, out var t, out bool head))
        {
            if (t != null)
            {
                bool back = Mathf.Abs(Mathf.DeltaAngle(yaw, t.Yaw)) < 70f;
                int dmg = back ? (heavy ? 100 : 80) : Mathf.RoundToInt((heavy ? W.heavy : W.dmg) * (head ? W.head : 1f));
                HitServerRpc(t.NetworkObjectId, dmg);
                hitT = 0.25f; hitHead = head || back;
                OgeAudio.Play2D(OgeAudio.stab, 0.7f);
            }
            else OgeAudio.Play2D(OgeAudio.click, 0.3f);
        }
        ShotServerRpc(ct.position, 3);
    }

    // ---------- ağ ----------
    [ServerRpc]
    void HitServerRpc(ulong targetId, int dmg)
    {
        var gs = OgeGameState.I;
        if (gs == null || gs.phase.Value != 1 || !Alive) return;
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var no)) return;
        var t = no.GetComponent<OgePlayer>();
        if (t == null || t == this || !t.Alive) return;
        t.hp.Value = Mathf.Max(0, t.hp.Value - Mathf.Clamp(dmg, 0, 100));
        if (t.hp.Value == 0) gs.OnKill(this, t);
    }

    [ServerRpc]
    void ShotServerRpc(Vector3 end, byte wi) { ShotClientRpc(end, wi); }

    [ClientRpc]
    void ShotClientRpc(Vector3 end, byte wi)
    {
        if (IsOwner) return;
        Vector3 from = gunT ? gunT.position + gunT.forward * 0.35f : transform.position + Vector3.up * 1.3f;
        if (wi < 3)
        {
            OgeAudio.Play3D(wi == 0 ? OgeAudio.rifle : wi == 1 ? OgeAudio.pistol : OgeAudio.sniper, from, 0.9f, 120f);
            OgeArt.I.Tracer(from, end);
            OgeArt.I.MuzzleLight(from);
        }
        else OgeAudio.Play3D(OgeAudio.swing, from, 0.6f, 20f);
    }

    [ClientRpc]
    public void RespawnClientRpc(Vector3 p, float y)
    {
        if (IsOwner) { Teleport(p, y); ResetLoadout(); }
        else { transform.position = p; lastPos = p; deadAng = 0; }
    }

    // ---------- ekran ----------
    bool menuHover;

    void OnGUI()
    {
        if (!IsOwner || !IsSpawned) return;
        OgeUI.Begin(out float W, out float H);
        var gs = OgeGameState.I;
        var cam = OgeArt.I.mainCam;
        bool locked = Cursor.lockState == CursorLockMode.Locked;

        if (zoom) DrawScope(W, H);
        if (hurtT > 0) OgeUI.Fill(new Rect(0, 0, W, H), new Color(0.9f, 0.1f, 0.1f, hurtT * 0.45f));

        // nişangah
        if (!zoom && Alive && w != 2)
        {
            float gap = 5 + (moving ? 6 : 0) + recoil * 120f, cx = W / 2, cy = H / 2;
            var c = new Color(1, 1, 1, 0.9f);
            OgeUI.Fill(new Rect(cx - gap - 8, cy - 1, 8, 2), c);
            OgeUI.Fill(new Rect(cx + gap, cy - 1, 8, 2), c);
            OgeUI.Fill(new Rect(cx - 1, cy - gap - 8, 2, 8), c);
            OgeUI.Fill(new Rect(cx - 1, cy + gap, 2, 8), c);
        }
        else if (!zoom && Alive) OgeUI.Fill(new Rect(W / 2 - 2, H / 2 - 2, 4, 4), Color.white);
        if (hitT > 0)
        {
            var hc = hitHead ? new Color(1f, 0.35f, 0.35f) : Color.white;
            var m = GUI.matrix;
            for (int i = 0; i < 4; i++)
            {
                GUIUtility.RotateAroundPivot(45 + i * 90, new Vector2(W / 2, H / 2) * (Screen.height / 720f));
                OgeUI.Fill(new Rect(W / 2 + 7, H / 2 - 1, 9, 2), hc);
                GUI.matrix = m;
            }
        }

        // rakip adı
        foreach (var p in All)
        {
            if (p == this || p == null || !p.Alive) continue;
            Vector3 hp3 = p.transform.position + Vector3.up * 2.05f;
            Vector3 sp = cam.WorldToScreenPoint(hp3);
            if (sp.z <= 0) continue;
            bool vis = !Physics.Linecast(cam.transform.position, hp3 - Vector3.up * 0.4f, out var lh, ~(1 << OgeArt.VMLayer), QueryTriggerInteraction.Ignore) || lh.collider.GetComponentInParent<OgePlayer>() == p;
            if (!vis) continue;
            float k = 720f / Screen.height;
            OgeUI.Shadow(new Rect(sp.x * k - 80, (Screen.height - sp.y) * k - 14, 160, 24), p.Name, OgeUI.Mid);
        }

        // üst: skor + süre
        if (gs != null)
        {
            int mine = Team == 0 ? gs.red.Value : gs.blue.Value, other = Team == 0 ? gs.blue.Value : gs.red.Value;
            string oname = OgeGameState.TeamName(1 - Team);
            GUI.Box(new Rect(W / 2 - 150, 14, 300, 40), GUIContent.none, OgeUI.Pill);
            GUI.Label(new Rect(W / 2 - 150, 14, 300, 40), "Sen  " + mine + "  –  " + other + "  " + oname, OgeUI.Mid);
            if (gs.phase.Value == 1)
            {
                int s = Mathf.CeilToInt(gs.left.Value);
                OgeUI.Shadow(new Rect(W / 2 - 60, 56, 120, 30), (s / 60) + ":" + (s % 60).ToString("00"), OgeUI.Mid);
            }
            string msg = gs.msg.Value.ToString();
            if (!string.IsNullOrEmpty(msg) && gs.phase.Value != 1)
            {
                GUI.Box(new Rect(W / 2 - 230, H * 0.3f, 460, 60), GUIContent.none, OgeUI.Banner);
                GUI.Label(new Rect(W / 2 - 230, H * 0.3f, 460, 60), msg, OgeUI.Mid);
            }
            if (gs.phase.Value == 0 && !string.IsNullOrEmpty(OgeNet.JoinCode))
                OgeUI.Shadow(new Rect(W / 2 - 230, H * 0.3f + 66, 460, 40), "Oda kodu: " + OgeNet.JoinCode + "  ·  Hasan bu kodla katılsın", OgeUI.Mid);
        }

        // can ve mermi
        GUI.Box(new Rect(20, H - 74, 170, 54), GUIContent.none, OgeUI.Pill);
        GUI.Label(new Rect(30, H - 74, 80, 54), hp.Value.ToString(), OgeUI.Big);
        OgeUI.Fill(new Rect(110, H - 50, 66, 8), new Color(1, 1, 1, 0.25f));
        OgeUI.Fill(new Rect(110, H - 50, 66 * hp.Value / 100f, 8), hp.Value > 30 ? new Color(0.45f, 0.85f, 0.55f) : new Color(1f, 0.4f, 0.4f));
        var Wd = OgeWeapons.W[w];
        GUI.Box(new Rect(W - 230, H - 74, 210, 54), GUIContent.none, OgeUI.Pill);
        GUI.Label(new Rect(W - 230, H - 80, 210, 44), Wd.melee ? "∞" : ammo[w] + " / " + Wd.mag, OgeUI.Big);
        GUI.Label(new Rect(W - 230, H - 42, 210, 20), relT > 0 ? "Dolduruluyor…" : Wd.name, OgeUI.Small);

        // menü (fare serbestken)
        menuHover = false;
        if (gs != null && gs.phase.Value == 3)
        {
            var r = new Rect(W / 2 - 160, H * 0.3f + 76, 320, 56);
            menuHover = r.Contains(Event.current.mousePosition);
            if (GUI.Button(r, "Rövanş", OgeUI.Btn)) gs.RematchServerRpc();
            if (GUI.Button(new Rect(W / 2 - 160, H * 0.3f + 140, 320, 44), "Lobiye dön", OgeUI.Btn2)) OgeNet.I.Leave();
        }
        else if (!locked)
        {
            float pw = 420, ph = 300, x = (W - pw) / 2, y = H * 0.45f;
            var pr = new Rect(x, y, pw, ph);
            menuHover = pr.Contains(Event.current.mousePosition);
            GUI.Box(pr, GUIContent.none, OgeUI.Panel);
            GUI.Label(new Rect(x, y + 14, pw, 34), "Oynamak için tıkla", OgeUI.Sub);
            GUI.Label(new Rect(x + 20, y + 50, pw - 40, 70), "WASD hareket · Space zıpla · Shift sessiz yürü\n1 tüfek · 2 tabanca · 3 keskin · 4 karambit\nSol tık ateş · Sağ tık dürbün / ağır bıçak · R doldur · Y incele", OgeUI.Small);
            GUI.Label(new Rect(x + 30, y + 130, 150, 30), "Hassasiyet", OgeUI.Text);
            float ns = GUI.HorizontalSlider(new Rect(x + 150, y + 140, pw - 190, 20), sens, 0.02f, 0.3f);
            if (!Mathf.Approximately(ns, sens)) { sens = ns; PlayerPrefs.SetFloat("oge_sens", sens); }
            if (GUI.Button(new Rect(x + 30, y + 180, pw - 60, 48), "Oyuna dön", OgeUI.Btn)) Cursor.lockState = CursorLockMode.Locked;
            if (GUI.Button(new Rect(x + 30, y + 238, pw - 60, 40), "Lobiye dön", OgeUI.Btn2)) OgeNet.I.Leave();
        }
    }

    static void DrawScope(float W, float H)
    {
        if (scopeTex == null)
        {
            int s = 512; scopeTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - s / 2f, dy = y - s / 2f, d = Mathf.Sqrt(dx * dx + dy * dy) / (s / 2f);
                    float a = Mathf.Clamp01((d - 0.96f) * 40f);
                    bool line = (Mathf.Abs(dx) < 1.2f || Mathf.Abs(dy) < 1.2f) && d < 0.96f;
                    scopeTex.SetPixel(x, y, line ? new Color(0, 0, 0, 0.9f) : new Color(0, 0, 0, a));
                }
            scopeTex.Apply();
        }
        float side = (W - H) / 2;
        OgeUI.Fill(new Rect(0, 0, side + 1, H), Color.black);
        OgeUI.Fill(new Rect(W - side - 1, 0, side + 2, H), Color.black);
        GUI.DrawTexture(new Rect(side, 0, H, H), scopeTex);
    }
}
