using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Raund ve skor: odayı kuran (host) yönetir, iki tarafa da senkron gider.
public class OgeGameState : NetworkBehaviour
{
    public static OgeGameState I;
    public const int WinScore = 5;
    public const float RoundTime = 90f;

    // 0 rakip bekleniyor · 1 raund · 2 raund sonu · 3 maç bitti
    public NetworkVariable<byte> phase = new NetworkVariable<byte>(0);
    public NetworkVariable<int> red = new NetworkVariable<int>(0);
    public NetworkVariable<int> blue = new NetworkVariable<int>(0);
    public NetworkVariable<float> left = new NetworkVariable<float>(RoundTime);
    public NetworkVariable<FixedString128Bytes> msg = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> winner = new NetworkVariable<int>(-1);

    float endT;

    public override void OnNetworkSpawn()
    {
        I = this;
        phase.OnValueChanged += (o, n) =>
        {
            if (n == 1) OgeAudio.Play2D(OgeAudio.roundStart, 0.5f);
            if (n == 3) Cursor.lockState = CursorLockMode.None;
        };
    }

    public override void OnNetworkDespawn() { if (I == this) I = null; }

    static FixedString128Bytes Fs(string s)
    {
        if (s.Length > 40) s = s.Substring(0, 40);
        return new FixedString128Bytes(s);
    }

    int Count()
    {
        int n = 0;
        foreach (var p in OgePlayer.All) if (p != null && p.IsSpawned) n++;
        return n;
    }

    public static OgePlayer Get(int team)
    {
        foreach (var p in OgePlayer.All) if (p != null && p.Team == team) return p;
        return null;
    }

    public static string TeamName(int team)
    {
        var p = Get(team);
        string n = p != null ? p.Name : "";
        return string.IsNullOrEmpty(n) ? (team == 0 ? "Kırmızı" : "Mavi") : n;
    }

    void Update()
    {
        if (!IsServer) return;
        float dt = Time.deltaTime;
        int n = Count();
        switch (phase.Value)
        {
            case 0:
                msg.Value = Fs("Rakip bekleniyor");
                if (n >= 2) { red.Value = 0; blue.Value = 0; winner.Value = -1; StartRound(); }
                break;
            case 1:
                if (n < 2) { phase.Value = 0; break; }
                left.Value = Mathf.Max(0, left.Value - dt);
                if (left.Value <= 0)
                {
                    var r = Get(0); var b = Get(1);
                    int w = r.hp.Value > b.hp.Value ? 0 : b.hp.Value > r.hp.Value ? 1 : -1;
                    EndRound(w, w < 0 ? "Süre doldu · berabere" : "Süre doldu · " + TeamName(w) + " kazandı");
                }
                break;
            case 2:
                if (n < 2) { phase.Value = 0; break; }
                endT -= dt;
                if (endT <= 0)
                {
                    if (red.Value >= WinScore || blue.Value >= WinScore)
                    {
                        winner.Value = red.Value >= WinScore ? 0 : 1;
                        phase.Value = 3;
                        msg.Value = Fs(TeamName(winner.Value) + " maçı kazandı!");
                    }
                    else StartRound();
                }
                break;
            case 3:
                if (n < 2) phase.Value = 0;
                break;
        }
    }

    void StartRound()
    {
        left.Value = RoundTime;
        msg.Value = Fs("");
        foreach (var p in OgePlayer.All)
        {
            if (p == null) continue;
            p.hp.Value = 100;
            p.RespawnClientRpc(OgeArt.Spawn(p.Team), OgeArt.SpawnYaw(p.Team));
        }
        phase.Value = 1;
    }

    public void OnKill(OgePlayer killer, OgePlayer victim)
    {
        if (phase.Value != 1) return;
        EndRound(killer.Team, TeamName(killer.Team) + " raundu kazandı");
    }

    void EndRound(int w, string m)
    {
        if (w == 0) red.Value++;
        else if (w == 1) blue.Value++;
        phase.Value = 2;
        endT = 3.5f;
        msg.Value = Fs(m);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RematchServerRpc()
    {
        if (phase.Value != 3) return;
        red.Value = 0; blue.Value = 0; winner.Value = -1;
        StartRound();
    }
}
