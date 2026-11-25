using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

// Lobi: oda kur / koda katıl. Bağlantı Unity Relay üzerinden (tarayıcıda WebSocket).
public class OgeNet : MonoBehaviour
{
    public static OgeNet I;
    public static string MyName = "";
    public static string JoinCode = "";
    public GameObject statePrefab;

    NetworkManager nm;
    UnityTransport tr;
    string codeIn = "", status = "";
    bool busy;

    void Start()
    {
        I = this;
        nm = GetComponent<NetworkManager>();
        tr = GetComponent<UnityTransport>();
        MyName = PlayerPrefs.GetString("oge_name", "");
        var pp = nm.NetworkConfig.PlayerPrefab;
        if (pp != null && !nm.NetworkConfig.Prefabs.Contains(pp)) nm.AddNetworkPrefab(pp);
        if (statePrefab != null) nm.AddNetworkPrefab(statePrefab);
        nm.OnServerStarted += () =>
        {
            var g = Instantiate(statePrefab);
            g.GetComponent<NetworkObject>().Spawn();
        };
        nm.OnClientDisconnectCallback += OnDisconnect;
    }

    bool InGame => nm != null && (nm.IsClient || nm.IsServer);

    void OnDisconnect(ulong id)
    {
        if (nm.IsServer) return; // rakip çıktı; oda açık kalır
        status = "Bağlantı koptu";
        Leave();
    }

    async System.Threading.Tasks.Task SignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void Host()
    {
        if (busy) return;
        busy = true; status = "Oda kuruluyor…";
        try
        {
            await SignIn();
            Allocation a = await RelayService.Instance.CreateAllocationAsync(1);
            tr.UseWebSockets = true;
            tr.SetRelayServerData(AllocationUtils.ToRelayServerData(a, "wss"));
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            status = nm.StartHost() ? "" : "Oda başlatılamadı";
        }
        catch (Exception e) { status = "Hata: " + e.Message; Debug.LogException(e); }
        busy = false;
    }

    public async void Join(string code)
    {
        if (busy) return;
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length < 4) { status = "Kodu yaz"; return; }
        busy = true; status = "Odaya bağlanılıyor…";
        try
        {
            await SignIn();
            JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(code);
            tr.UseWebSockets = true;
            tr.SetRelayServerData(AllocationUtils.ToRelayServerData(a, "wss"));
            JoinCode = code;
            status = nm.StartClient() ? "" : "Bağlanılamadı";
        }
        catch (Exception e) { status = "Oda bulunamadı. Kodu kontrol et."; Debug.LogException(e); }
        busy = false;
    }

    public void Leave()
    {
        if (nm.IsListening || nm.IsClient || nm.IsServer) nm.Shutdown();
        JoinCode = "";
        Cursor.lockState = CursorLockMode.None;
        if (OgeViewModel.I) OgeViewModel.I.Show(false);
    }

    void Update()
    {
        if (InGame || OgeArt.I == null) return;
        // lobide kamera haritanın üstünde yavaşça döner
        float t = Time.time * 0.06f;
        var c = OgeArt.I.mainCam.transform;
        c.position = new Vector3(Mathf.Sin(t) * 48f, 24f, Mathf.Cos(t) * 48f);
        c.rotation = Quaternion.LookRotation(new Vector3(0, -2f, 0) - c.position);
        OgeArt.I.mainCam.fieldOfView = 60f;
    }

    void OnGUI()
    {
        if (InGame) return;
        OgeUI.Begin(out float W, out float H);
        float pw = 440, ph = 400, x = (W - pw) / 2, y = (H - ph) / 2;
        GUI.Box(new Rect(x, y, pw, ph), GUIContent.none, OgeUI.Panel);
        GUI.Label(new Rect(x, y + 24, pw, 50), "Oge Macerası", OgeUI.Title);
        GUI.Label(new Rect(x, y + 72, pw, 26), "1v1 Kapışma", OgeUI.Sub);

        GUI.Label(new Rect(x + 40, y + 118, 120, 30), "Adın", OgeUI.Text);
        string nn = GUI.TextField(new Rect(x + 120, y + 116, pw - 160, 34), MyName, 12, OgeUI.Field);
        if (nn != MyName) { MyName = nn; PlayerPrefs.SetString("oge_name", MyName); }

        bool shutting = nm != null && nm.ShutdownInProgress;
        GUI.enabled = !busy && !shutting;
        if (GUI.Button(new Rect(x + 40, y + 170, pw - 80, 52), "Oda kur", OgeUI.Btn)) Host();

        GUI.Label(new Rect(x, y + 234, pw, 24), "ya da Hasan'ın verdiği kodla katıl", OgeUI.Small);
        codeIn = GUI.TextField(new Rect(x + 40, y + 264, 200, 46), codeIn.ToUpperInvariant(), 8, OgeUI.CodeField);
        if (GUI.Button(new Rect(x + 252, y + 264, pw - 292, 46), "Katıl", OgeUI.Btn2)) Join(codeIn);
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(x, y + 326, pw, 28), status, OgeUI.Small);
        GUI.Label(new Rect(x, y + ph - 36, pw, 24), "Unity sürümü · deneme", OgeUI.Small);
    }
}
